namespace AiSiteFiller.V2.Presentation
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.btnStart = new System.Windows.Forms.Button();
            this.txtProduct = new System.Windows.Forms.TextBox();
            this.rtbLogs = new System.Windows.Forms.RichTextBox();
            this.lblProduct = new System.Windows.Forms.Label();
            this.lblPending = new System.Windows.Forms.Label();
            this.lblCompleted = new System.Windows.Forms.Label();
            this.lblError = new System.Windows.Forms.Label();
            this.dgvArticles = new System.Windows.Forms.DataGridView();
            this.dgvQueueDetails = new System.Windows.Forms.DataGridView();
            this.pbCoverPreview = new System.Windows.Forms.PictureBox();

            // Компоненты пагинации, карусели и статус-панелей по ТЗ
            this.btnPrevPage = new System.Windows.Forms.Button();
            this.btnNextPage = new System.Windows.Forms.Button();
            this.lblPageInfo = new System.Windows.Forms.Label();
            this.btnPrevImg = new System.Windows.Forms.Button();
            this.btnNextImg = new System.Windows.Forms.Button();
            this.lblImgInfo = new System.Windows.Forms.Label();
            this.gbHealth = new System.Windows.Forms.GroupBox();
            this.lblBegetStats = new System.Windows.Forms.Label();
            this.lblYandexStats = new System.Windows.Forms.Label();
            this.chkEnableWorker = new System.Windows.Forms.CheckBox();

            ((System.ComponentModel.ISupportInitialize)(this.dgvArticles)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvQueueDetails)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbCoverPreview)).BeginInit();
            this.gbHealth.SuspendLayout();
            this.SuspendLayout();
            // Кнопка Старта
            this.btnStart.Location = new System.Drawing.Point(820, 10);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(150, 25);
            this.btnStart.Text = "Запустить конвейер";
            this.btnStart.Click += new System.EventHandler(this.BtnStart_Click);

            // Поле ввода товара
            this.txtProduct.Location = new System.Drawing.Point(120, 12);
            this.txtProduct.Name = "txtProduct";
            this.txtProduct.Size = new System.Drawing.Size(680, 23);
            this.txtProduct.Text = "DeLonghi Magnifica Start ECAM22.110.B";

            // Метка товара
            this.lblProduct.Location = new System.Drawing.Point(12, 15);
            this.lblProduct.Size = new System.Drawing.Size(102, 15);
            this.lblProduct.Text = "Название товара:";

            // Настройка панели здоровья Beget и Яндекса
            this.gbHealth.Controls.Add(this.lblBegetStats);
            this.gbHealth.Controls.Add(this.lblYandexStats);
            this.gbHealth.Controls.Add(this.chkEnableWorker);
            this.gbHealth.Location = new System.Drawing.Point(12, 50);
            this.gbHealth.Name = "gbHealth";
            this.gbHealth.Size = new System.Drawing.Size(250, 160);
            this.gbHealth.Text = "Здоровье сети и фабрики";

            this.lblBegetStats.Location = new System.Drawing.Point(10, 25);
            this.lblBegetStats.Size = new System.Drawing.Size(230, 40);
            this.lblBegetStats.Text = "Beget: 450.50 руб.\nДиск: [██████░░░] 60%";

            this.lblYandexStats.Location = new System.Drawing.Point(10, 75);
            this.lblYandexStats.Size = new System.Drawing.Size(230, 40);
            this.lblYandexStats.Text = "Яндекс: Критических ошибок нет\nИндекс: 1,420 страниц";

            this.chkEnableWorker.Location = new System.Drawing.Point(10, 125);
            this.chkEnableWorker.Size = new System.Drawing.Size(230, 20);
            this.chkEnableWorker.Text = "Авто-выкат по расписанию";
            this.chkEnableWorker.Checked = true;

            // Смещенное и сжатое поле логов Serilog
            this.rtbLogs.BackColor = System.Drawing.Color.Black;
            this.rtbLogs.ForeColor = System.Drawing.Color.LightGray;
            this.rtbLogs.Location = new System.Drawing.Point(275, 57);
            this.rtbLogs.Name = "rtbLogs";
            this.rtbLogs.ReadOnly = true;
            this.rtbLogs.Size = new System.Drawing.Size(695, 153);
            // Таблица 1: Список всех статей (Слева снизу)
            this.dgvArticles.AllowUserToAddRows = false;
            this.dgvArticles.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvArticles.Location = new System.Drawing.Point(12, 230);
            this.dgvArticles.MultiSelect = false;
            this.dgvArticles.Name = "dgvArticles";
            this.dgvArticles.ReadOnly = true;
            this.dgvArticles.RowHeadersVisible = false;
            this.dgvArticles.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvArticles.Size = new System.Drawing.Size(430, 310);
            this.dgvArticles.SelectionChanged += new System.EventHandler(this.DgvArticles_SelectionChanged);

            // Кнопки и инфо пагинации грида статей
            this.btnPrevPage.Location = new System.Drawing.Point(12, 545);
            this.btnPrevPage.Size = new System.Drawing.Size(40, 25);
            this.btnPrevPage.Text = "◀";
            this.btnPrevPage.Click += new System.EventHandler(this.BtnPrevPage_Click);

            this.lblPageInfo.Location = new System.Drawing.Point(58, 550);
            this.lblPageInfo.Size = new System.Drawing.Size(120, 20);
            this.lblPageInfo.Text = "Страница: 1 / 1";
            this.lblPageInfo.TextAlign = System.Drawing.ContentAlignment.TopCenter;

            this.btnNextPage.Location = new System.Drawing.Point(184, 545);
            this.btnNextPage.Size = new System.Drawing.Size(40, 25);
            this.btnNextPage.Text = "▶";
            this.btnNextPage.Click += new System.EventHandler(this.BtnNextPage_Click);

            // PictureBox: Альбомный формат обложек (1024x576) — Смещен в самый низ
            this.pbCoverPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pbCoverPreview.Location = new System.Drawing.Point(460, 370); // 🔥 НОВАЯ ПОЗИЦИЯ: В самом низу
            this.pbCoverPreview.Name = "pbCoverPreview";
            this.pbCoverPreview.Size = new System.Drawing.Size(510, 200);
            this.pbCoverPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbCoverPreview.BackColor = System.Drawing.Color.DimGray;

            // Кнопки карусели картинок — Смещены под PictureBox
            this.btnPrevImg.Location = new System.Drawing.Point(460, 575); // 🔥 Смещено вниз
            this.btnPrevImg.Size = new System.Drawing.Size(40, 25);
            this.btnPrevImg.Text = "◀";
            this.btnPrevImg.Click += new System.EventHandler(this.BtnPrevImg_Click);

            this.lblImgInfo.Location = new System.Drawing.Point(510, 580); // 🔥 Смещено вниз
            this.lblImgInfo.Size = new System.Drawing.Size(410, 20);
            this.lblImgInfo.Text = "Изображение: 0 из 0";
            this.lblImgInfo.TextAlign = System.Drawing.ContentAlignment.TopCenter;

            this.btnNextImg.Location = new System.Drawing.Point(930, 575); // 🔥 Смещено вниз
            this.btnNextImg.Size = new System.Drawing.Size(40, 25);
            this.btnNextImg.Text = "▶";
            this.btnNextImg.Click += new System.EventHandler(this.BtnNextImg_Click);

            // Таблица 2: Детали по платформам веера — Поднята НАВЕРХ над PictureBox
            this.dgvQueueDetails.AllowUserToAddRows = false;
            this.dgvQueueDetails.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dgvQueueDetails.Location = new System.Drawing.Point(460, 230); // 🔥 НОВАЯ ПОЗИЦИЯ: Сразу под логами, над картинкой
            this.dgvQueueDetails.Name = "dgvQueueDetails";
            this.dgvQueueDetails.ReadOnly = true;
            this.dgvQueueDetails.RowHeadersVisible = false;
            this.dgvQueueDetails.Size = new System.Drawing.Size(510, 130); // Немного увеличили высоту для читаемости


            // Суммарные счетчики в подвале
            this.lblPending.Location = new System.Drawing.Point(12, 595);
            this.lblPending.Size = new System.Drawing.Size(150, 15);
            this.lblCompleted.Location = new System.Drawing.Point(200, 595);
            this.lblCompleted.Size = new System.Drawing.Size(150, 15);
            this.lblError.Location = new System.Drawing.Point(400, 595);
            this.lblError.Size = new System.Drawing.Size(150, 15);

            // Конфигурация MainForm
            this.ClientSize = new System.Drawing.Size(984, 621);
            this.Controls.Add(this.gbHealth);
            this.Controls.Add(this.btnPrevPage);
            this.Controls.Add(this.lblPageInfo);
            this.Controls.Add(this.btnNextPage);
            this.Controls.Add(this.btnPrevImg);
            this.Controls.Add(this.lblImgInfo);
            this.Controls.Add(this.btnNextImg);
            this.Controls.Add(this.pbCoverPreview);
            this.Controls.Add(this.dgvQueueDetails);
            this.Controls.Add(this.dgvArticles);
            this.Controls.Add(this.lblError);
            this.Controls.Add(this.lblCompleted);
            this.Controls.Add(this.lblPending);
            this.Controls.Add(this.lblProduct);
            this.Controls.Add(this.rtbLogs);
            this.Controls.Add(this.txtProduct);
            this.Controls.Add(this.btnStart);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AiSiteFiller V2.0 — Панель Управления Фабрикой Контента";

            ((System.ComponentModel.ISupportInitialize)(this.dgvArticles)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvQueueDetails)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbCoverPreview)).EndInit();
            this.gbHealth.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.TextBox txtProduct;
        private System.Windows.Forms.RichTextBox rtbLogs;
        private System.Windows.Forms.Label lblProduct;
        private System.Windows.Forms.Label lblPending;
        private System.Windows.Forms.Label lblCompleted;
        private System.Windows.Forms.Label lblError;
        private System.Windows.Forms.DataGridView dgvArticles;
        private System.Windows.Forms.DataGridView dgvQueueDetails;
        private System.Windows.Forms.PictureBox pbCoverPreview;
        private System.Windows.Forms.Button btnPrevPage;
        private System.Windows.Forms.Button btnNextPage;
        private System.Windows.Forms.Label lblPageInfo;
        private System.Windows.Forms.Button btnPrevImg;
        private System.Windows.Forms.Button btnNextImg;
        private System.Windows.Forms.Label lblImgInfo;
        private System.Windows.Forms.GroupBox gbHealth;
        private System.Windows.Forms.Label lblBegetStats;
        private System.Windows.Forms.Label lblYandexStats;
        private System.Windows.Forms.CheckBox chkEnableWorker;
    }
}
