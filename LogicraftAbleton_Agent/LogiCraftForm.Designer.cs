namespace LogicraftAbleton
{
	partial class LogicraftForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
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
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LogicraftForm));
			this.logBox = new System.Windows.Forms.RichTextBox();
			this.CheckboxLogging = new System.Windows.Forms.CheckBox();
			this.ButtonReconnect = new System.Windows.Forms.Button();
			this.CheckboxHoldMode = new System.Windows.Forms.CheckBox();
			this.TextboxTimerDuration = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.TextboxWheelFactor = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.CheckboxKeyboardRatchetEnabled = new System.Windows.Forms.CheckBox();
			this.LogicraftNotifyTray = new System.Windows.Forms.NotifyIcon(this.components);
			this.SuspendLayout();
			// 
			// logBox
			// 
			this.logBox.Location = new System.Drawing.Point(3, 2);
			this.logBox.Name = "logBox";
			this.logBox.Size = new System.Drawing.Size(616, 545);
			this.logBox.TabIndex = 0;
			this.logBox.Text = "Welcome";
			// 
			// CheckboxLogging
			// 
			this.CheckboxLogging.AutoSize = true;
			this.CheckboxLogging.Location = new System.Drawing.Point(625, 10);
			this.CheckboxLogging.Name = "CheckboxLogging";
			this.CheckboxLogging.Size = new System.Drawing.Size(100, 17);
			this.CheckboxLogging.TabIndex = 2;
			this.CheckboxLogging.Text = "Enable Logging";
			this.CheckboxLogging.UseVisualStyleBackColor = true;
			this.CheckboxLogging.CheckedChanged += new System.EventHandler(this.CheckboxLogging_CheckedChanged);
			// 
			// ButtonReconnect
			// 
			this.ButtonReconnect.Location = new System.Drawing.Point(636, 125);
			this.ButtonReconnect.Name = "ButtonReconnect";
			this.ButtonReconnect.Size = new System.Drawing.Size(75, 23);
			this.ButtonReconnect.TabIndex = 3;
			this.ButtonReconnect.Text = "Reconnect";
			this.ButtonReconnect.UseVisualStyleBackColor = true;
			this.ButtonReconnect.Click += new System.EventHandler(this.ButtonReconnect_Click);
			// 
			// CheckboxHoldMode
			// 
			this.CheckboxHoldMode.AutoSize = true;
			this.CheckboxHoldMode.Location = new System.Drawing.Point(625, 33);
			this.CheckboxHoldMode.Name = "CheckboxHoldMode";
			this.CheckboxHoldMode.Size = new System.Drawing.Size(114, 17);
			this.CheckboxHoldMode.TabIndex = 4;
			this.CheckboxHoldMode.Text = "Enable Hold Mode";
			this.CheckboxHoldMode.UseVisualStyleBackColor = true;
			this.CheckboxHoldMode.CheckedChanged += new System.EventHandler(this.CheckboxHoldMode_CheckedChanged);
			// 
			// TextboxTimerDuration
			// 
			this.TextboxTimerDuration.Location = new System.Drawing.Point(717, 74);
			this.TextboxTimerDuration.Name = "TextboxTimerDuration";
			this.TextboxTimerDuration.Size = new System.Drawing.Size(51, 20);
			this.TextboxTimerDuration.TabIndex = 5;
			this.TextboxTimerDuration.Text = "500";
			this.TextboxTimerDuration.TextChanged += new System.EventHandler(this.TextboxTimerDuration_TextChanged);
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(625, 77);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(76, 13);
			this.label1.TabIndex = 6;
			this.label1.Text = "Timer Duration";
			// 
			// TextboxWheelFactor
			// 
			this.TextboxWheelFactor.Location = new System.Drawing.Point(717, 101);
			this.TextboxWheelFactor.Name = "TextboxWheelFactor";
			this.TextboxWheelFactor.Size = new System.Drawing.Size(51, 20);
			this.TextboxWheelFactor.TabIndex = 7;
			this.TextboxWheelFactor.Text = "4.4";
			this.TextboxWheelFactor.TextChanged += new System.EventHandler(this.TextboxWheelFactor_TextChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(625, 104);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(91, 13);
			this.label2.TabIndex = 8;
			this.label2.Text = "Wheel Sim Factor";
			// 
			// CheckboxKeyboardRatchetEnabled
			// 
			this.CheckboxKeyboardRatchetEnabled.AutoSize = true;
			this.CheckboxKeyboardRatchetEnabled.Location = new System.Drawing.Point(625, 56);
			this.CheckboxKeyboardRatchetEnabled.Name = "CheckboxKeyboardRatchetEnabled";
			this.CheckboxKeyboardRatchetEnabled.Size = new System.Drawing.Size(148, 17);
			this.CheckboxKeyboardRatchetEnabled.TabIndex = 9;
			this.CheckboxKeyboardRatchetEnabled.Text = "Enable Ratchet for Key In";
			this.CheckboxKeyboardRatchetEnabled.UseVisualStyleBackColor = true;
			this.CheckboxKeyboardRatchetEnabled.CheckedChanged += new System.EventHandler(this.CheckboxKeyboardRatchetEnabled_CheckedChanged);
			// 
			// LogicraftNotifyTray
			// 
			this.LogicraftNotifyTray.Icon = ((System.Drawing.Icon)(resources.GetObject("LogicraftNotifyTray.Icon")));
			this.LogicraftNotifyTray.Text = "Logicraft for Ableton";
			this.LogicraftNotifyTray.Visible = true;
			this.LogicraftNotifyTray.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.LogicraftNotifyTray_MouseDoubleClick);
			// 
			// LogicraftForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(778, 635);
			this.Controls.Add(this.CheckboxKeyboardRatchetEnabled);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.TextboxWheelFactor);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.TextboxTimerDuration);
			this.Controls.Add(this.CheckboxHoldMode);
			this.Controls.Add(this.ButtonReconnect);
			this.Controls.Add(this.CheckboxLogging);
			this.Controls.Add(this.logBox);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Margin = new System.Windows.Forms.Padding(2);
			this.Name = "LogicraftForm";
			this.Text = "Logicraft for Ableton";
			this.Resize += new System.EventHandler(this.LogicraftForm_Resize);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox CheckboxLogging;
		private System.Windows.Forms.RichTextBox logBox;
		private System.Windows.Forms.Button ButtonReconnect;
		private System.Windows.Forms.CheckBox CheckboxHoldMode;
		private System.Windows.Forms.TextBox TextboxTimerDuration;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox TextboxWheelFactor;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.CheckBox CheckboxKeyboardRatchetEnabled;
		private System.Windows.Forms.NotifyIcon LogicraftNotifyTray;
	}
}