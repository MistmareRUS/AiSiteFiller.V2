using System;
using System.IO;
using System.Data;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AiSiteFiller.V2.Application.Interfaces;
using AiSiteFiller.V2.Domain.Enums;
using AiSiteFiller.V2.Infrastructure.Services;
using AiSiteFiller.V2.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AiSiteFiller.V2.Presentation
{
    public partial class MainForm : Form
    {
        private readonly IContentOrchestrator _orchestrator;
        private readonly ILogger<MainForm> _logger;
        private bool _isInitialLoading = true;

        // Переменные состояния пагинации СУБД и карусели картинок Mongo
        private int _currentPage = 1;
        private const int PageSize = 10;
        private int _totalPropertiesCount = 0;
        private List<string> _currentArticleImages = new List<string>();
        private int _currentImageIndex = 0;

        public MainForm(IContentOrchestrator orchestrator, ILogger<MainForm> logger)
        {
            InitializeComponent();
            _orchestrator = orchestrator;
            _logger = logger;

            // Подключаем Serilog-приемник к нашему текстовому полю
            UiLogSink.OnLogReceived += UpdateLogBox;
        }

        protected override async void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            _logger.LogInformation("Инициализация СУБД контуров данных при запуске интерфейса...");
            try
            {
                using var scope = Program.ServiceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                // Накатываем миграции Postgres в фоновом UI-потоке без зависания формы
                await dbContext.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Сбой автоматических миграций.");
            }

            await RefreshAllUiDataAsync();
            _isInitialLoading = false;
        }
        private async void BtnStart_Click(object sender, EventArgs e)
        {
            string productName = txtProduct.Text.Trim();
            if (string.IsNullOrWhiteSpace(productName)) return;

            btnStart.Enabled = false;
            try
            {
                await _orchestrator.GenerateAndQueueArticleAsync(productName);
                await RefreshAllUiDataAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка ручной генерации контента.");
            }
            finally
            {
                btnStart.Enabled = true;
            }
        }

        private async void DgvArticles_SelectionChanged(object sender, EventArgs e)
        {
            if (_isInitialLoading || dgvArticles.SelectedRows.Count == 0) return;
            long articleId = Convert.ToInt64(dgvArticles.SelectedRows[0].Cells["ArticleId"].Value);

            try
            {
                using var scope = Program.ServiceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var queueTasks = await dbContext.PublishingQueue
                    .Where(t => t.ArticleId == articleId)
                    .Select(t => new { t.Platform, t.Status, t.LiveUrl })
                    .ToListAsync();
                dgvQueueDetails.DataSource = queueTasks;

                var article = await dbContext.Articles.FindAsync(articleId);
                _currentArticleImages.Clear();
                if (article != null)
                {
                    _currentArticleImages.AddRange(article.ImagesMetadata.Values);
                }

                _currentImageIndex = 0;
                await DisplayCurrentImageAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка при прокликивании строки статьи {Id}", articleId);
            }
        }
        private async Task DisplayCurrentImageAsync()
        {
            if (_currentArticleImages.Count == 0)
            {
                pbCoverPreview.Image = null;
                lblImgInfo.Text = "Изображения отсутствуют";
                return;
            }

            try
            {
                using var scope = Program.ServiceProvider.CreateScope();
                var mediaRepo = scope.ServiceProvider.GetRequiredService<IMediaRepository>();
                string currentMongoId = _currentArticleImages[_currentImageIndex];

                byte[] imgBytes = await mediaRepo.GetImageBytesAsync(currentMongoId);
                using var ms = new MemoryStream(imgBytes);
                pbCoverPreview.Image = Image.FromStream(ms);
                lblImgInfo.Text = $"Изображение: {_currentImageIndex + 1} из {_currentArticleImages.Count}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка карусели картинок.");
            }
        }

        private async void BtnPrevImg_Click(object sender, EventArgs e)
        {
            if (_currentArticleImages.Count <= 1) return;
            _currentImageIndex = (_currentImageIndex == 0) ? _currentArticleImages.Count - 1 : _currentImageIndex - 1;
            await DisplayCurrentImageAsync();
        }

        private async void BtnNextImg_Click(object sender, EventArgs e)
        {
            if (_currentArticleImages.Count <= 1) return;
            _currentImageIndex = (_currentImageIndex == _currentArticleImages.Count - 1) ? 0 : _currentImageIndex + 1;
            await DisplayCurrentImageAsync();
        }

        private async void BtnPrevPage_Click(object sender, EventArgs e)
        {
            if (_currentPage <= 1) return;
            _currentPage--;
            await RefreshAllUiDataAsync();
        }

        private async void BtnNextPage_Click(object sender, EventArgs e)
        {
            int maxPages = (int)Math.Ceiling((double)_totalPropertiesCount / PageSize);
            if (_currentPage >= maxPages) return;
            _currentPage++;
            await RefreshAllUiDataAsync();
        }
        private async Task RefreshAllUiDataAsync()
        {
            try
            {
                using var scope = Program.ServiceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var queueRepo = scope.ServiceProvider.GetRequiredService<IQueueRepository>();

                _totalPropertiesCount = await dbContext.Articles.CountAsync();
                int totalPages = Math.Max(1, (int)Math.Ceiling((double)_totalPropertiesCount / PageSize));
                lblPageInfo.Text = $"Страница: {_currentPage} / {totalPages}";

                // Выборка с пагинацией (по 10 статей)
                var articlesList = await dbContext.Articles
                    .OrderByDescending(a => a.CreatedAt)
                    .Skip((_currentPage - 1) * PageSize)
                    .Take(PageSize)
                    .Select(a => new { a.ArticleId, a.ProductName, a.CreatedAt })
                    .ToListAsync();
                dgvArticles.DataSource = articlesList;

                // Считывание сводки из PostgreSQL
                int pendingCount = await queueRepo.GetCountByStatusAsync(PublicationStatus.Pending);
                int completedCount = await queueRepo.GetCountByStatusAsync(PublicationStatus.Posted);
                int errorCount = await queueRepo.GetCountByStatusAsync(PublicationStatus.Error);

                lblPending.Text = $"В очереди: {pendingCount} постов";
                lblCompleted.Text = $"Успешно: {completedCount} постов";
                lblError.Text = $"Сбои: {errorCount} задач";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Ошибка обновления глобальных данных UI.");
            }
        }

        private void UpdateLogBox(string message)
        {
            if (rtbLogs.InvokeRequired)
            {
                rtbLogs.BeginInvoke(new Action<string>(UpdateLogBox), message);
                return;
            }
            rtbLogs.AppendText(message + Environment.NewLine);
            rtbLogs.SelectionStart = rtbLogs.Text.Length;
            rtbLogs.ScrollToCaret();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Защита от утечек памяти в статических событиях рантайма
            UiLogSink.OnLogReceived -= UpdateLogBox;
            base.OnFormClosing(e);
        }
    }
}
