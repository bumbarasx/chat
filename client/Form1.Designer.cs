namespace client01
{
    partial class Form1
    {
        /// <summary>
        /// Обязательная переменная конструктора.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Освободить все используемые ресурсы.
        /// </summary>
        /// <param name="disposing">истинно, если управляемый ресурс должен быть удален; иначе ложно.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Код, автоматически созданный конструктором форм Windows

        /// <summary>
        /// Требуемый метод для поддержки конструктора — не изменяйте 
        /// содержимое этого метода с помощью редактора кода.
        /// </summary>
        private void InitializeComponent()
        {
            this.textPole = new System.Windows.Forms.RichTextBox();
            this.textMessage = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textPole
            // 
            this.textPole.BackColor = System.Drawing.Color.White;
            this.textPole.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textPole.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.textPole.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.textPole.Location = new System.Drawing.Point(12, 12);
            this.textPole.Name = "textPole";
            this.textPole.Size = new System.Drawing.Size(385, 175);
            this.textPole.TabIndex = 0;
            this.textPole.Text = "";
            // 
            // textMessage
            // 
            this.textMessage.BackColor = System.Drawing.Color.White;
            this.textMessage.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textMessage.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.textMessage.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.textMessage.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.textMessage.Location = new System.Drawing.Point(12, 197);
            this.textMessage.Margin = new System.Windows.Forms.Padding(0);
            this.textMessage.MaxLength = 700;
            this.textMessage.Name = "textMessage";
            this.textMessage.Size = new System.Drawing.Size(293, 14);
            this.textMessage.TabIndex = 1;
            this.textMessage.WordWrap = false;
            this.textMessage.TextChanged += new System.EventHandler(this.textMessage_TextChanged);
            this.textMessage.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textMessage_KeyDown);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(327, 194);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 3;
            this.button1.Text = "send";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(406, 220);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.textMessage);
            this.Controls.Add(this.textPole);
            this.ForeColor = System.Drawing.Color.Black;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Shown += new System.EventHandler(this.Form1_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RichTextBox textPole;
        private System.Windows.Forms.TextBox textMessage;
        private System.Windows.Forms.Button button1;
    }
}

