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
      this.feedIdTextBox = new System.Windows.Forms.TextBox();
      this.requestLocationButton = new System.Windows.Forms.Button();
      this.resultTextBox = new System.Windows.Forms.TextBox();
      this.dest1ComboBox = new System.Windows.Forms.ComboBox();
      this.dest2ComboBox = new System.Windows.Forms.ComboBox();
      this.dest3ComboBox = new System.Windows.Forms.ComboBox();
      this.dest4ComboBox = new System.Windows.Forms.ComboBox();
      this.dest5ComboBox = new System.Windows.Forms.ComboBox();
      this.dest6ComboBox = new System.Windows.Forms.ComboBox();
      this.defaultFeeds = new System.Windows.Forms.Button();
      this.clearFeeds = new System.Windows.Forms.Button();
      this.groupBox1 = new System.Windows.Forms.GroupBox();
      this.groupBox2 = new System.Windows.Forms.GroupBox();
      this.editSampleXmlButton = new System.Windows.Forms.Button();
      this.xmlSourceRadioButton = new System.Windows.Forms.RadioButton();
      this.inetSourceRadioButton = new System.Windows.Forms.RadioButton();
      this.clearResultButton = new System.Windows.Forms.Button();
      this.button1 = new System.Windows.Forms.Button();
      this.label1 = new System.Windows.Forms.Label();
      this.groupBox3 = new System.Windows.Forms.GroupBox();
      this.comboBox1 = new System.Windows.Forms.ComboBox();
      this.button2 = new System.Windows.Forms.Button();
      this.comboBox2 = new System.Windows.Forms.ComboBox();
      this.button3 = new System.Windows.Forms.Button();
      this.comboBox3 = new System.Windows.Forms.ComboBox();
      this.comboBox4 = new System.Windows.Forms.ComboBox();
      this.comboBox5 = new System.Windows.Forms.ComboBox();
      this.comboBox6 = new System.Windows.Forms.ComboBox();
      this.groupBox4 = new System.Windows.Forms.GroupBox();
      this.button4 = new System.Windows.Forms.Button();
      this.radioButton1 = new System.Windows.Forms.RadioButton();
      this.radioButton2 = new System.Windows.Forms.RadioButton();
      this.textBox2 = new System.Windows.Forms.TextBox();
      this.button5 = new System.Windows.Forms.Button();
      this.tabControl2 = new System.Windows.Forms.TabControl();
      this.tabPage3 = new System.Windows.Forms.TabPage();
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
      this.groupBox1.SuspendLayout();
      this.groupBox2.SuspendLayout();
      this.groupBox3.SuspendLayout();
      this.groupBox4.SuspendLayout();
      this.tabControl2.SuspendLayout();
      this.tabPage3.SuspendLayout();
      this.tabPage4.SuspendLayout();
      this.SuspendLayout();
      // 
      // feedIdTextBox
      // 
      this.feedIdTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.feedIdTextBox.Location = new System.Drawing.Point(112, 19);
      this.feedIdTextBox.Name = "feedIdTextBox";
      this.feedIdTextBox.Size = new System.Drawing.Size(274, 20);
      this.feedIdTextBox.TabIndex = 1;
      this.feedIdTextBox.Text = "0k6O9bM1Yu6XtghZaRlupbKUmvl5xkm0I";
      this.feedIdTextBox.Enter += new System.EventHandler(this.feedIdTextBox_Enter);
      // 
      // requestLocationButton
      // 
      this.requestLocationButton.Location = new System.Drawing.Point(6, 199);
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
      this.resultTextBox.Location = new System.Drawing.Point(12, 303);
      this.resultTextBox.Multiline = true;
      this.resultTextBox.Name = "resultTextBox";
      this.resultTextBox.ReadOnly = true;
      this.resultTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
      this.resultTextBox.Size = new System.Drawing.Size(848, 284);
      this.resultTextBox.TabIndex = 5;
      this.resultTextBox.WordWrap = false;
      // 
      // dest1ComboBox
      // 
      this.dest1ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.dest1ComboBox.FormattingEnabled = true;
      this.dest1ComboBox.Items.AddRange(new object[] {
            "Feed_1_0",
            "Feed_1_0_undoc",
            "Feed_2_0"});
      this.dest1ComboBox.Location = new System.Drawing.Point(6, 19);
      this.dest1ComboBox.Name = "dest1ComboBox";
      this.dest1ComboBox.Size = new System.Drawing.Size(111, 21);
      this.dest1ComboBox.TabIndex = 6;
      // 
      // dest2ComboBox
      // 
      this.dest2ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.dest2ComboBox.FormattingEnabled = true;
      this.dest2ComboBox.Items.AddRange(new object[] {
            "Feed_1_0",
            "Feed_1_0_undoc",
            "Feed_2_0"});
      this.dest2ComboBox.Location = new System.Drawing.Point(123, 19);
      this.dest2ComboBox.Name = "dest2ComboBox";
      this.dest2ComboBox.Size = new System.Drawing.Size(121, 21);
      this.dest2ComboBox.TabIndex = 7;
      // 
      // dest3ComboBox
      // 
      this.dest3ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.dest3ComboBox.FormattingEnabled = true;
      this.dest3ComboBox.Items.AddRange(new object[] {
            "Feed_1_0",
            "Feed_1_0_undoc",
            "Feed_2_0"});
      this.dest3ComboBox.Location = new System.Drawing.Point(250, 19);
      this.dest3ComboBox.Name = "dest3ComboBox";
      this.dest3ComboBox.Size = new System.Drawing.Size(121, 21);
      this.dest3ComboBox.TabIndex = 8;
      // 
      // dest4ComboBox
      // 
      this.dest4ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.dest4ComboBox.FormattingEnabled = true;
      this.dest4ComboBox.Items.AddRange(new object[] {
            "Feed_1_0",
            "Feed_1_0_undoc",
            "Feed_2_0"});
      this.dest4ComboBox.Location = new System.Drawing.Point(377, 19);
      this.dest4ComboBox.Name = "dest4ComboBox";
      this.dest4ComboBox.Size = new System.Drawing.Size(121, 21);
      this.dest4ComboBox.TabIndex = 9;
      // 
      // dest5ComboBox
      // 
      this.dest5ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.dest5ComboBox.FormattingEnabled = true;
      this.dest5ComboBox.Items.AddRange(new object[] {
            "Feed_1_0",
            "Feed_1_0_undoc",
            "Feed_2_0"});
      this.dest5ComboBox.Location = new System.Drawing.Point(504, 19);
      this.dest5ComboBox.Name = "dest5ComboBox";
      this.dest5ComboBox.Size = new System.Drawing.Size(121, 21);
      this.dest5ComboBox.TabIndex = 10;
      // 
      // dest6ComboBox
      // 
      this.dest6ComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.dest6ComboBox.FormattingEnabled = true;
      this.dest6ComboBox.Items.AddRange(new object[] {
            "Feed_1_0",
            "Feed_1_0_undoc",
            "Feed_2_0"});
      this.dest6ComboBox.Location = new System.Drawing.Point(631, 19);
      this.dest6ComboBox.Name = "dest6ComboBox";
      this.dest6ComboBox.Size = new System.Drawing.Size(121, 21);
      this.dest6ComboBox.TabIndex = 11;
      // 
      // defaultFeeds
      // 
      this.defaultFeeds.Location = new System.Drawing.Point(6, 46);
      this.defaultFeeds.Name = "defaultFeeds";
      this.defaultFeeds.Size = new System.Drawing.Size(75, 23);
      this.defaultFeeds.TabIndex = 12;
      this.defaultFeeds.Text = "Default";
      this.defaultFeeds.UseVisualStyleBackColor = true;
      this.defaultFeeds.Click += new System.EventHandler(this.defaultFeeds_Click);
      // 
      // clearFeeds
      // 
      this.clearFeeds.Location = new System.Drawing.Point(87, 46);
      this.clearFeeds.Name = "clearFeeds";
      this.clearFeeds.Size = new System.Drawing.Size(75, 23);
      this.clearFeeds.TabIndex = 13;
      this.clearFeeds.Text = "Clear";
      this.clearFeeds.UseVisualStyleBackColor = true;
      this.clearFeeds.Click += new System.EventHandler(this.clearFeeds_Click);
      // 
      // groupBox1
      // 
      this.groupBox1.Controls.Add(this.dest1ComboBox);
      this.groupBox1.Controls.Add(this.clearFeeds);
      this.groupBox1.Controls.Add(this.dest2ComboBox);
      this.groupBox1.Controls.Add(this.defaultFeeds);
      this.groupBox1.Controls.Add(this.dest3ComboBox);
      this.groupBox1.Controls.Add(this.dest6ComboBox);
      this.groupBox1.Controls.Add(this.dest4ComboBox);
      this.groupBox1.Controls.Add(this.dest5ComboBox);
      this.groupBox1.Location = new System.Drawing.Point(6, 112);
      this.groupBox1.Name = "groupBox1";
      this.groupBox1.Size = new System.Drawing.Size(831, 81);
      this.groupBox1.TabIndex = 14;
      this.groupBox1.TabStop = false;
      this.groupBox1.Text = "Destination Feeds";
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
      // clearResultButton
      // 
      this.clearResultButton.Location = new System.Drawing.Point(144, 199);
      this.clearResultButton.Name = "clearResultButton";
      this.clearResultButton.Size = new System.Drawing.Size(75, 23);
      this.clearResultButton.TabIndex = 16;
      this.clearResultButton.Text = "&Clear result";
      this.clearResultButton.UseVisualStyleBackColor = true;
      this.clearResultButton.Click += new System.EventHandler(this.clearResultButton_Click);
      // 
      // button1
      // 
      this.button1.Location = new System.Drawing.Point(6, 199);
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
      // groupBox3
      // 
      this.groupBox3.Controls.Add(this.comboBox1);
      this.groupBox3.Controls.Add(this.button2);
      this.groupBox3.Controls.Add(this.comboBox2);
      this.groupBox3.Controls.Add(this.button3);
      this.groupBox3.Controls.Add(this.comboBox3);
      this.groupBox3.Controls.Add(this.comboBox4);
      this.groupBox3.Controls.Add(this.comboBox5);
      this.groupBox3.Controls.Add(this.comboBox6);
      this.groupBox3.Location = new System.Drawing.Point(6, 112);
      this.groupBox3.Name = "groupBox3";
      this.groupBox3.Size = new System.Drawing.Size(831, 81);
      this.groupBox3.TabIndex = 14;
      this.groupBox3.TabStop = false;
      this.groupBox3.Text = "Destination Feeds";
      // 
      // comboBox1
      // 
      this.comboBox1.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.comboBox1.FormattingEnabled = true;
      this.comboBox1.Items.AddRange(new object[] {
            "Feed_1_0",
            "Feed_1_0_undoc",
            "Feed_2_0"});
      this.comboBox1.Location = new System.Drawing.Point(6, 19);
      this.comboBox1.Name = "comboBox1";
      this.comboBox1.Size = new System.Drawing.Size(111, 21);
      this.comboBox1.TabIndex = 6;
      // 
      // button2
      // 
      this.button2.Location = new System.Drawing.Point(87, 46);
      this.button2.Name = "button2";
      this.button2.Size = new System.Drawing.Size(75, 23);
      this.button2.TabIndex = 13;
      this.button2.Text = "Clear";
      this.button2.UseVisualStyleBackColor = true;
      this.button2.Click += new System.EventHandler(this.clearFeeds_Click);
      // 
      // comboBox2
      // 
      this.comboBox2.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.comboBox2.FormattingEnabled = true;
      this.comboBox2.Items.AddRange(new object[] {
            "Feed_1_0",
            "Feed_1_0_undoc",
            "Feed_2_0"});
      this.comboBox2.Location = new System.Drawing.Point(123, 19);
      this.comboBox2.Name = "comboBox2";
      this.comboBox2.Size = new System.Drawing.Size(121, 21);
      this.comboBox2.TabIndex = 7;
      // 
      // button3
      // 
      this.button3.Location = new System.Drawing.Point(6, 46);
      this.button3.Name = "button3";
      this.button3.Size = new System.Drawing.Size(75, 23);
      this.button3.TabIndex = 12;
      this.button3.Text = "Default";
      this.button3.UseVisualStyleBackColor = true;
      this.button3.Click += new System.EventHandler(this.defaultFeeds_Click);
      // 
      // comboBox3
      // 
      this.comboBox3.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.comboBox3.FormattingEnabled = true;
      this.comboBox3.Items.AddRange(new object[] {
            "Feed_1_0",
            "Feed_1_0_undoc",
            "Feed_2_0"});
      this.comboBox3.Location = new System.Drawing.Point(250, 19);
      this.comboBox3.Name = "comboBox3";
      this.comboBox3.Size = new System.Drawing.Size(121, 21);
      this.comboBox3.TabIndex = 8;
      // 
      // comboBox4
      // 
      this.comboBox4.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.comboBox4.FormattingEnabled = true;
      this.comboBox4.Items.AddRange(new object[] {
            "Feed_1_0",
            "Feed_1_0_undoc",
            "Feed_2_0"});
      this.comboBox4.Location = new System.Drawing.Point(631, 19);
      this.comboBox4.Name = "comboBox4";
      this.comboBox4.Size = new System.Drawing.Size(121, 21);
      this.comboBox4.TabIndex = 11;
      // 
      // comboBox5
      // 
      this.comboBox5.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.comboBox5.FormattingEnabled = true;
      this.comboBox5.Items.AddRange(new object[] {
            "Feed_1_0",
            "Feed_1_0_undoc",
            "Feed_2_0"});
      this.comboBox5.Location = new System.Drawing.Point(377, 19);
      this.comboBox5.Name = "comboBox5";
      this.comboBox5.Size = new System.Drawing.Size(121, 21);
      this.comboBox5.TabIndex = 9;
      // 
      // comboBox6
      // 
      this.comboBox6.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
      this.comboBox6.FormattingEnabled = true;
      this.comboBox6.Items.AddRange(new object[] {
            "Feed_1_0",
            "Feed_1_0_undoc",
            "Feed_2_0"});
      this.comboBox6.Location = new System.Drawing.Point(504, 19);
      this.comboBox6.Name = "comboBox6";
      this.comboBox6.Size = new System.Drawing.Size(121, 21);
      this.comboBox6.TabIndex = 10;
      // 
      // groupBox4
      // 
      this.groupBox4.Controls.Add(this.button4);
      this.groupBox4.Controls.Add(this.radioButton1);
      this.groupBox4.Controls.Add(this.radioButton2);
      this.groupBox4.Controls.Add(this.textBox2);
      this.groupBox4.Location = new System.Drawing.Point(6, 6);
      this.groupBox4.Name = "groupBox4";
      this.groupBox4.Size = new System.Drawing.Size(831, 100);
      this.groupBox4.TabIndex = 15;
      this.groupBox4.TabStop = false;
      this.groupBox4.Text = "Source";
      // 
      // button4
      // 
      this.button4.Location = new System.Drawing.Point(112, 62);
      this.button4.Name = "button4";
      this.button4.Size = new System.Drawing.Size(274, 23);
      this.button4.TabIndex = 4;
      this.button4.Text = "See/Modify Sample XML...";
      this.button4.UseVisualStyleBackColor = true;
      this.button4.Click += new System.EventHandler(this.editSampleXmlButton_Click);
      // 
      // radioButton1
      // 
      this.radioButton1.AutoSize = true;
      this.radioButton1.Location = new System.Drawing.Point(15, 65);
      this.radioButton1.Name = "radioButton1";
      this.radioButton1.Size = new System.Drawing.Size(88, 17);
      this.radioButton1.TabIndex = 3;
      this.radioButton1.TabStop = true;
      this.radioButton1.Text = "Sample XML:";
      this.radioButton1.UseVisualStyleBackColor = true;
      // 
      // radioButton2
      // 
      this.radioButton2.AutoSize = true;
      this.radioButton2.Checked = true;
      this.radioButton2.Location = new System.Drawing.Point(15, 20);
      this.radioButton2.Name = "radioButton2";
      this.radioButton2.Size = new System.Drawing.Size(64, 17);
      this.radioButton2.TabIndex = 2;
      this.radioButton2.TabStop = true;
      this.radioButton2.Text = "Feed Id:";
      this.radioButton2.UseVisualStyleBackColor = true;
      // 
      // textBox2
      // 
      this.textBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.textBox2.Location = new System.Drawing.Point(112, 19);
      this.textBox2.Name = "textBox2";
      this.textBox2.Size = new System.Drawing.Size(274, 20);
      this.textBox2.TabIndex = 1;
      this.textBox2.Text = "0k6O9bM1Yu6XtghZaRlupbKUmvl5xkm0I";
      this.textBox2.Enter += new System.EventHandler(this.feedIdTextBox_Enter);
      // 
      // button5
      // 
      this.button5.Location = new System.Drawing.Point(144, 199);
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
      this.tabControl2.Size = new System.Drawing.Size(855, 278);
      this.tabControl2.TabIndex = 17;
      // 
      // tabPage3
      // 
      this.tabPage3.Controls.Add(this.groupBox4);
      this.tabPage3.Controls.Add(this.button5);
      this.tabPage3.Controls.Add(this.requestLocationButton);
      this.tabPage3.Controls.Add(this.button1);
      this.tabPage3.Controls.Add(this.clearResultButton);
      this.tabPage3.Controls.Add(this.groupBox3);
      this.tabPage3.Controls.Add(this.groupBox2);
      this.tabPage3.Controls.Add(this.groupBox1);
      this.tabPage3.Location = new System.Drawing.Point(4, 22);
      this.tabPage3.Name = "tabPage3";
      this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
      this.tabPage3.Size = new System.Drawing.Size(847, 252);
      this.tabPage3.TabIndex = 0;
      this.tabPage3.Text = "Location Request Test";
      this.tabPage3.UseVisualStyleBackColor = true;
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
      this.groupBox1.ResumeLayout(false);
      this.groupBox2.ResumeLayout(false);
      this.groupBox2.PerformLayout();
      this.groupBox3.ResumeLayout(false);
      this.groupBox4.ResumeLayout(false);
      this.groupBox4.PerformLayout();
      this.tabControl2.ResumeLayout(false);
      this.tabPage3.ResumeLayout(false);
      this.tabPage4.ResumeLayout(false);
      this.tabPage4.PerformLayout();
      this.ResumeLayout(false);
      this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.TextBox feedIdTextBox;
    private System.Windows.Forms.Button requestLocationButton;
    private System.Windows.Forms.TextBox resultTextBox;
    private System.Windows.Forms.ComboBox dest1ComboBox;
    private System.Windows.Forms.ComboBox dest2ComboBox;
    private System.Windows.Forms.ComboBox dest3ComboBox;
    private System.Windows.Forms.ComboBox dest4ComboBox;
    private System.Windows.Forms.ComboBox dest5ComboBox;
    private System.Windows.Forms.ComboBox dest6ComboBox;
    private System.Windows.Forms.Button defaultFeeds;
    private System.Windows.Forms.Button clearFeeds;
    private System.Windows.Forms.GroupBox groupBox1;
    private System.Windows.Forms.GroupBox groupBox2;
    private System.Windows.Forms.Button editSampleXmlButton;
    private System.Windows.Forms.RadioButton xmlSourceRadioButton;
    private System.Windows.Forms.RadioButton inetSourceRadioButton;
    private System.Windows.Forms.Button clearResultButton;
    private System.Windows.Forms.Button button1;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.GroupBox groupBox3;
    private System.Windows.Forms.ComboBox comboBox1;
    private System.Windows.Forms.Button button2;
    private System.Windows.Forms.ComboBox comboBox2;
    private System.Windows.Forms.Button button3;
    private System.Windows.Forms.ComboBox comboBox3;
    private System.Windows.Forms.ComboBox comboBox4;
    private System.Windows.Forms.ComboBox comboBox5;
    private System.Windows.Forms.ComboBox comboBox6;
    private System.Windows.Forms.GroupBox groupBox4;
    private System.Windows.Forms.Button button4;
    private System.Windows.Forms.RadioButton radioButton1;
    private System.Windows.Forms.RadioButton radioButton2;
    private System.Windows.Forms.TextBox textBox2;
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
  }
}

