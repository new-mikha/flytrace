namespace LocationRequestTest
{
  partial class MainForm
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose( bool disposing )
    {
      if ( disposing && ( components != null ) )
      {
        components.Dispose( );
      }
      base.Dispose( disposing );
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent( )
    {
      this.requestLocationButton = new System.Windows.Forms.Button();
      this.resultTextBox = new System.Windows.Forms.TextBox();
      this.clearResultButton = new System.Windows.Forms.Button();
      this.button1 = new System.Windows.Forms.Button();
      this.label1 = new System.Windows.Forms.Label();
      this.button5 = new System.Windows.Forms.Button();
      this.tabControl2 = new System.Windows.Forms.TabControl();
      this.tabPage3 = new System.Windows.Forms.TabPage();
      this.button6 = new System.Windows.Forms.Button();
      this.label3 = new System.Windows.Forms.Label();
      this.tabPage4 = new System.Windows.Forms.TabPage();
      this.revgenShutownAndIncrementButton = new System.Windows.Forms.Button();
      this.revgenGetCurrentButton = new System.Windows.Forms.Button();
      this.revgenIncrement100Button = new System.Windows.Forms.Button();
      this.revgenInrcement10Button = new System.Windows.Forms.Button();
      this.revgenIncrement1Button = new System.Windows.Forms.Button();
      this.revgenShutdownButton = new System.Windows.Forms.Button();
      this.revgenInitButton = new System.Windows.Forms.Button();
      this.revgenFilePathTextBox = new System.Windows.Forms.TextBox();
      this.label2 = new System.Windows.Forms.Label();
      this.feedIdTextBox = new System.Windows.Forms.TextBox();
      this.inetSourceRadioButton = new System.Windows.Forms.RadioButton();
      this.xmlSourceRadioButton = new System.Windows.Forms.RadioButton();
      this.editSampleXmlButton = new System.Windows.Forms.Button();
      this.groupBox2 = new System.Windows.Forms.GroupBox();
      this.tabControl2.SuspendLayout();
      this.tabPage3.SuspendLayout();
      this.tabPage4.SuspendLayout();
      this.groupBox2.SuspendLayout();
      this.SuspendLayout();
      // 
      // requestLocationButton
      // 
      this.requestLocationButton.Location = new System.Drawing.Point(7, 131);
      this.requestLocationButton.Name = "requestLocationButton";
      this.requestLocationButton.Size = new System.Drawing.Size(114, 23);
      this.requestLocationButton.TabIndex = 3;
      this.requestLocationButton.Text = "Request Location";
      this.requestLocationButton.UseVisualStyleBackColor = true;
      this.requestLocationButton.Click += new System.EventHandler(this.requestLocationButton_Click);
      // 
      // resultTextBox
      // 
      this.resultTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.resultTextBox.Location = new System.Drawing.Point(12, 238);
      this.resultTextBox.Multiline = true;
      this.resultTextBox.Name = "resultTextBox";
      this.resultTextBox.ReadOnly = true;
      this.resultTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
      this.resultTextBox.Size = new System.Drawing.Size(848, 349);
      this.resultTextBox.TabIndex = 5;
      this.resultTextBox.WordWrap = false;
      // 
      // clearResultButton
      // 
      this.clearResultButton.Location = new System.Drawing.Point(145, 131);
      this.clearResultButton.Name = "clearResultButton";
      this.clearResultButton.Size = new System.Drawing.Size(75, 23);
      this.clearResultButton.TabIndex = 16;
      this.clearResultButton.Text = "&Clear result";
      this.clearResultButton.UseVisualStyleBackColor = true;
      this.clearResultButton.Click += new System.EventHandler(this.clearResultButton_Click);
      // 
      // button1
      // 
      this.button1.Location = new System.Drawing.Point(7, 131);
      this.button1.Name = "button1";
      this.button1.Size = new System.Drawing.Size(114, 23);
      this.button1.TabIndex = 3;
      this.button1.Text = "Request Location";
      this.button1.UseVisualStyleBackColor = true;
      this.button1.Click += new System.EventHandler(this.requestLocationButton_Click);
      // 
      // label1
      // 
      this.label1.AutoSize = true;
      this.label1.Location = new System.Drawing.Point(9, 287);
      this.label1.Name = "label1";
      this.label1.Size = new System.Drawing.Size(37, 13);
      this.label1.TabIndex = 4;
      this.label1.Text = "&Result";
      // 
      // button5
      // 
      this.button5.Location = new System.Drawing.Point(145, 131);
      this.button5.Name = "button5";
      this.button5.Size = new System.Drawing.Size(75, 23);
      this.button5.TabIndex = 16;
      this.button5.Text = "&Clear result";
      this.button5.UseVisualStyleBackColor = true;
      this.button5.Click += new System.EventHandler(this.clearResultButton_Click);
      // 
      // tabControl2
      // 
      this.tabControl2.Controls.Add(this.tabPage3);
      this.tabControl2.Controls.Add(this.tabPage4);
      this.tabControl2.Location = new System.Drawing.Point(12, 6);
      this.tabControl2.Name = "tabControl2";
      this.tabControl2.SelectedIndex = 0;
      this.tabControl2.Size = new System.Drawing.Size(855, 207);
      this.tabControl2.TabIndex = 17;
      // 
      // tabPage3
      // 
      this.tabPage3.Controls.Add(this.button6);
      this.tabPage3.Controls.Add(this.label3);
      this.tabPage3.Controls.Add(this.button5);
      this.tabPage3.Controls.Add(this.requestLocationButton);
      this.tabPage3.Controls.Add(this.button1);
      this.tabPage3.Controls.Add(this.clearResultButton);
      this.tabPage3.Controls.Add(this.groupBox2);
      this.tabPage3.Location = new System.Drawing.Point(4, 22);
      this.tabPage3.Name = "tabPage3";
      this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
      this.tabPage3.Size = new System.Drawing.Size(847, 181);
      this.tabPage3.TabIndex = 0;
      this.tabPage3.Text = "Location Request Test";
      this.tabPage3.UseVisualStyleBackColor = true;
      // 
      // button6
      // 
      this.button6.Location = new System.Drawing.Point(335, 131);
      this.button6.Name = "button6";
      this.button6.Size = new System.Drawing.Size(75, 23);
      this.button6.TabIndex = 18;
      this.button6.Text = "button6";
      this.button6.UseVisualStyleBackColor = true;
      this.button6.Click += new System.EventHandler(this.button6_Click);
      // 
      // label3
      // 
      this.label3.AutoSize = true;
      this.label3.Location = new System.Drawing.Point(270, 136);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(35, 13);
      this.label3.TabIndex = 17;
      this.label3.Text = "label3";
      // 
      // tabPage4
      // 
      this.tabPage4.Controls.Add(this.revgenShutownAndIncrementButton);
      this.tabPage4.Controls.Add(this.revgenGetCurrentButton);
      this.tabPage4.Controls.Add(this.revgenIncrement100Button);
      this.tabPage4.Controls.Add(this.revgenInrcement10Button);
      this.tabPage4.Controls.Add(this.revgenIncrement1Button);
      this.tabPage4.Controls.Add(this.revgenShutdownButton);
      this.tabPage4.Controls.Add(this.revgenInitButton);
      this.tabPage4.Controls.Add(this.revgenFilePathTextBox);
      this.tabPage4.Controls.Add(this.label2);
      this.tabPage4.Location = new System.Drawing.Point(4, 22);
      this.tabPage4.Name = "tabPage4";
      this.tabPage4.Padding = new System.Windows.Forms.Padding(3);
      this.tabPage4.Size = new System.Drawing.Size(847, 252);
      this.tabPage4.TabIndex = 1;
      this.tabPage4.Text = "RevGen test";
      this.tabPage4.UseVisualStyleBackColor = true;
      // 
      // revgenShutownAndIncrementButton
      // 
      this.revgenShutownAndIncrementButton.Location = new System.Drawing.Point(117, 96);
      this.revgenShutownAndIncrementButton.Name = "revgenShutownAndIncrementButton";
      this.revgenShutownAndIncrementButton.Size = new System.Drawing.Size(171, 23);
      this.revgenShutownAndIncrementButton.TabIndex = 8;
      this.revgenShutownAndIncrementButton.Text = "Shutdown && Increment";
      this.revgenShutownAndIncrementButton.UseVisualStyleBackColor = true;
      this.revgenShutownAndIncrementButton.Click += new System.EventHandler(this.revgenShutownAndIncrementButton_Click);
      // 
      // revgenGetCurrentButton
      // 
      this.revgenGetCurrentButton.Location = new System.Drawing.Point(9, 96);
      this.revgenGetCurrentButton.Name = "revgenGetCurrentButton";
      this.revgenGetCurrentButton.Size = new System.Drawing.Size(75, 23);
      this.revgenGetCurrentButton.TabIndex = 7;
      this.revgenGetCurrentButton.Text = "Get Current";
      this.revgenGetCurrentButton.UseVisualStyleBackColor = true;
      this.revgenGetCurrentButton.Click += new System.EventHandler(this.revgenGetCurrentButton_Click);
      // 
      // revgenIncrement100Button
      // 
      this.revgenIncrement100Button.Location = new System.Drawing.Point(340, 45);
      this.revgenIncrement100Button.Name = "revgenIncrement100Button";
      this.revgenIncrement100Button.Size = new System.Drawing.Size(85, 23);
      this.revgenIncrement100Button.TabIndex = 6;
      this.revgenIncrement100Button.Text = "Increment 100";
      this.revgenIncrement100Button.UseVisualStyleBackColor = true;
      this.revgenIncrement100Button.Click += new System.EventHandler(this.revgenIncrement100Button_Click);
      // 
      // revgenInrcement10Button
      // 
      this.revgenInrcement10Button.Location = new System.Drawing.Point(249, 45);
      this.revgenInrcement10Button.Name = "revgenInrcement10Button";
      this.revgenInrcement10Button.Size = new System.Drawing.Size(85, 23);
      this.revgenInrcement10Button.TabIndex = 5;
      this.revgenInrcement10Button.Text = "Increment 10";
      this.revgenInrcement10Button.UseVisualStyleBackColor = true;
      this.revgenInrcement10Button.Click += new System.EventHandler(this.revgenInrcement10Button_Click);
      // 
      // revgenIncrement1Button
      // 
      this.revgenIncrement1Button.Location = new System.Drawing.Point(168, 45);
      this.revgenIncrement1Button.Name = "revgenIncrement1Button";
      this.revgenIncrement1Button.Size = new System.Drawing.Size(75, 23);
      this.revgenIncrement1Button.TabIndex = 4;
      this.revgenIncrement1Button.Text = "Increment 1";
      this.revgenIncrement1Button.UseVisualStyleBackColor = true;
      this.revgenIncrement1Button.Click += new System.EventHandler(this.revgenIncrement1Button_Click);
      // 
      // revgenShutdownButton
      // 
      this.revgenShutdownButton.Location = new System.Drawing.Point(87, 45);
      this.revgenShutdownButton.Name = "revgenShutdownButton";
      this.revgenShutdownButton.Size = new System.Drawing.Size(75, 23);
      this.revgenShutdownButton.TabIndex = 3;
      this.revgenShutdownButton.Text = "&Shutdown";
      this.revgenShutdownButton.UseVisualStyleBackColor = true;
      this.revgenShutdownButton.Click += new System.EventHandler(this.revgenShutdownButton_Click);
      // 
      // revgenInitButton
      // 
      this.revgenInitButton.Location = new System.Drawing.Point(6, 45);
      this.revgenInitButton.Name = "revgenInitButton";
      this.revgenInitButton.Size = new System.Drawing.Size(75, 23);
      this.revgenInitButton.TabIndex = 2;
      this.revgenInitButton.Text = "&Init";
      this.revgenInitButton.UseVisualStyleBackColor = true;
      this.revgenInitButton.Click += new System.EventHandler(this.revgenInitButton_Click);
      // 
      // revgenFilePathTextBox
      // 
      this.revgenFilePathTextBox.Location = new System.Drawing.Point(6, 19);
      this.revgenFilePathTextBox.Name = "revgenFilePathTextBox";
      this.revgenFilePathTextBox.Size = new System.Drawing.Size(303, 20);
      this.revgenFilePathTextBox.TabIndex = 1;
      this.revgenFilePathTextBox.Text = "c:\\temp\\revgentest.txt";
      // 
      // label2
      // 
      this.label2.AutoSize = true;
      this.label2.Location = new System.Drawing.Point(6, 3);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(68, 13);
      this.label2.TabIndex = 0;
      this.label2.Text = "&Persisting file";
      // 
      // feedIdTextBox
      // 
      this.feedIdTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.feedIdTextBox.Location = new System.Drawing.Point(112, 19);
      this.feedIdTextBox.Name = "feedIdTextBox";
      this.feedIdTextBox.Size = new System.Drawing.Size(274, 20);
      this.feedIdTextBox.TabIndex = 1;
      this.feedIdTextBox.Enter += new System.EventHandler(this.feedIdTextBox_Enter);
      // 
      // inetSourceRadioButton
      // 
      this.inetSourceRadioButton.AutoSize = true;
      this.inetSourceRadioButton.Checked = true;
      this.inetSourceRadioButton.Location = new System.Drawing.Point(15, 20);
      this.inetSourceRadioButton.Name = "inetSourceRadioButton";
      this.inetSourceRadioButton.Size = new System.Drawing.Size(64, 17);
      this.inetSourceRadioButton.TabIndex = 2;
      this.inetSourceRadioButton.TabStop = true;
      this.inetSourceRadioButton.Text = "Feed Id:";
      this.inetSourceRadioButton.UseVisualStyleBackColor = true;
      // 
      // xmlSourceRadioButton
      // 
      this.xmlSourceRadioButton.AutoSize = true;
      this.xmlSourceRadioButton.Location = new System.Drawing.Point(15, 65);
      this.xmlSourceRadioButton.Name = "xmlSourceRadioButton";
      this.xmlSourceRadioButton.Size = new System.Drawing.Size(88, 17);
      this.xmlSourceRadioButton.TabIndex = 3;
      this.xmlSourceRadioButton.TabStop = true;
      this.xmlSourceRadioButton.Text = "Sample XML:";
      this.xmlSourceRadioButton.UseVisualStyleBackColor = true;
      // 
      // editSampleXmlButton
      // 
      this.editSampleXmlButton.Location = new System.Drawing.Point(112, 62);
      this.editSampleXmlButton.Name = "editSampleXmlButton";
      this.editSampleXmlButton.Size = new System.Drawing.Size(274, 23);
      this.editSampleXmlButton.TabIndex = 4;
      this.editSampleXmlButton.Text = "See/Modify Sample XML...";
      this.editSampleXmlButton.UseVisualStyleBackColor = true;
      this.editSampleXmlButton.Click += new System.EventHandler(this.editSampleXmlButton_Click);
      // 
      // groupBox2
      // 
      this.groupBox2.Controls.Add(this.editSampleXmlButton);
      this.groupBox2.Controls.Add(this.xmlSourceRadioButton);
      this.groupBox2.Controls.Add(this.inetSourceRadioButton);
      this.groupBox2.Controls.Add(this.feedIdTextBox);
      this.groupBox2.Location = new System.Drawing.Point(6, 6);
      this.groupBox2.Name = "groupBox2";
      this.groupBox2.Size = new System.Drawing.Size(831, 100);
      this.groupBox2.TabIndex = 15;
      this.groupBox2.TabStop = false;
      this.groupBox2.Text = "Source";
      // 
      // MainForm
      // 
      this.AcceptButton = this.requestLocationButton;
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.ClientSize = new System.Drawing.Size(872, 599);
      this.Controls.Add(this.tabControl2);
      this.Controls.Add(this.resultTextBox);
      this.Controls.Add(this.label1);
      this.Name = "MainForm";
      this.Text = "FT Test UI";
      this.Load += new System.EventHandler(this.MainForm_Load);
      this.tabControl2.ResumeLayout(false);
      this.tabPage3.ResumeLayout(false);
      this.tabPage3.PerformLayout();
      this.tabPage4.ResumeLayout(false);
      this.tabPage4.PerformLayout();
      this.groupBox2.ResumeLayout(false);
      this.groupBox2.PerformLayout();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Button requestLocationButton;
    private System.Windows.Forms.TextBox resultTextBox;
    private System.Windows.Forms.Button clearResultButton;
    private System.Windows.Forms.Button button1;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.Button button5;
    private System.Windows.Forms.TabControl tabControl2;
    private System.Windows.Forms.TabPage tabPage3;
    private System.Windows.Forms.TabPage tabPage4;
    private System.Windows.Forms.Button revgenShutownAndIncrementButton;
    private System.Windows.Forms.Button revgenGetCurrentButton;
    private System.Windows.Forms.Button revgenIncrement100Button;
    private System.Windows.Forms.Button revgenInrcement10Button;
    private System.Windows.Forms.Button revgenIncrement1Button;
    private System.Windows.Forms.Button revgenShutdownButton;
    private System.Windows.Forms.Button revgenInitButton;
    private System.Windows.Forms.TextBox revgenFilePathTextBox;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Button button6;
    private System.Windows.Forms.GroupBox groupBox2;
    private System.Windows.Forms.Button editSampleXmlButton;
    private System.Windows.Forms.RadioButton xmlSourceRadioButton;
    private System.Windows.Forms.RadioButton inetSourceRadioButton;
    private System.Windows.Forms.TextBox feedIdTextBox;
  }
}

