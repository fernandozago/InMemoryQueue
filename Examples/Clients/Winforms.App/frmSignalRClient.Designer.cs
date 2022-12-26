namespace GrpcClient2
{
    partial class frmSignalRClient
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.formsPlot1 = new ScottPlot.FormsPlot();
            this.btnPub = new System.Windows.Forms.Button();
            this.btnAddConsumer = new System.Windows.Forms.Button();
            this.btnRemoveConsumer = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.button2 = new System.Windows.Forms.Button();
            this.btnResetCounters = new System.Windows.Forms.Button();
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(147, 51);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 4;
            this.button1.Text = "Refresh";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // formsPlot1
            // 
            this.formsPlot1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.formsPlot1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.formsPlot1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.formsPlot1.Location = new System.Drawing.Point(0, 0);
            this.formsPlot1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.formsPlot1.Name = "formsPlot1";
            this.formsPlot1.Size = new System.Drawing.Size(627, 493);
            this.formsPlot1.TabIndex = 12;
            // 
            // btnPub
            // 
            this.btnPub.Location = new System.Drawing.Point(147, 22);
            this.btnPub.Name = "btnPub";
            this.btnPub.Size = new System.Drawing.Size(75, 23);
            this.btnPub.TabIndex = 13;
            this.btnPub.Text = "Publish";
            this.btnPub.UseVisualStyleBackColor = true;
            this.btnPub.Click += new System.EventHandler(this.btnPub_Click);
            // 
            // btnAddConsumer
            // 
            this.btnAddConsumer.Location = new System.Drawing.Point(6, 22);
            this.btnAddConsumer.Name = "btnAddConsumer";
            this.btnAddConsumer.Size = new System.Drawing.Size(95, 23);
            this.btnAddConsumer.TabIndex = 26;
            this.btnAddConsumer.Text = "Add Client";
            this.btnAddConsumer.UseVisualStyleBackColor = true;
            this.btnAddConsumer.Click += new System.EventHandler(this.btnAddConsumer_Click);
            // 
            // btnRemoveConsumer
            // 
            this.btnRemoveConsumer.Location = new System.Drawing.Point(6, 51);
            this.btnRemoveConsumer.Name = "btnRemoveConsumer";
            this.btnRemoveConsumer.Size = new System.Drawing.Size(95, 23);
            this.btnRemoveConsumer.TabIndex = 27;
            this.btnRemoveConsumer.Text = "Remove Client";
            this.btnRemoveConsumer.UseVisualStyleBackColor = true;
            this.btnRemoveConsumer.Click += new System.EventHandler(this.btnRemoveConsumer_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.button2);
            this.groupBox1.Controls.Add(this.btnResetCounters);
            this.groupBox1.Controls.Add(this.btnAddConsumer);
            this.groupBox1.Controls.Add(this.btnRemoveConsumer);
            this.groupBox1.Controls.Add(this.btnPub);
            this.groupBox1.Controls.Add(this.button1);
            this.groupBox1.Location = new System.Drawing.Point(13, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(1098, 82);
            this.groupBox1.TabIndex = 28;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Consumers";
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(265, 51);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 29;
            this.button2.Text = "Start";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // btnResetCounters
            // 
            this.btnResetCounters.Location = new System.Drawing.Point(265, 22);
            this.btnResetCounters.Name = "btnResetCounters";
            this.btnResetCounters.Size = new System.Drawing.Size(75, 23);
            this.btnResetCounters.TabIndex = 28;
            this.btnResetCounters.Text = "Reset";
            this.btnResetCounters.UseVisualStyleBackColor = true;
            this.btnResetCounters.Click += new System.EventHandler(this.btnResetCounters_Click);
            // 
            // propertyGrid1
            // 
            this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid1.Location = new System.Drawing.Point(0, 0);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.PropertySort = System.Windows.Forms.PropertySort.Categorized;
            this.propertyGrid1.Size = new System.Drawing.Size(468, 493);
            this.propertyGrid1.TabIndex = 33;
            this.propertyGrid1.ToolbarVisible = false;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(12, 100);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.propertyGrid1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.formsPlot1);
            this.splitContainer1.Size = new System.Drawing.Size(1099, 493);
            this.splitContainer1.SplitterDistance = 468;
            this.splitContainer1.TabIndex = 34;
            // 
            // frmSignalRClient
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1123, 634);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.groupBox1);
            this.Name = "frmSignalRClient";
            this.Text = "Form1";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form1_FormClosed);
            this.groupBox1.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private Button button1;
        private ScottPlot.FormsPlot formsPlot1;
        private Button btnPub;
        private Button btnAddConsumer;
        private Button btnRemoveConsumer;
        private GroupBox groupBox1;
        private PropertyGrid propertyGrid1;
        private SplitContainer splitContainer1;
        private Button btnResetCounters;
        private Button button2;
    }
}