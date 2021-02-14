using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Windows.Forms;
using Commons.Music.Midi;
using LogicraftAbleton.Model;
using Newtonsoft.Json;
using WebSocketSharp;
using Timer = System.Timers.Timer;


namespace LogicraftAbleton
{
	public partial class LogicraftForm : Form
	{
		private const int Mhousewheelunit = 120;
		private string _sessionId = "";

		[DllImport("kernel32.dll")]
		public static extern bool ProcessIdToSessionId(uint dwProcessID, int pSessionID);

		[DllImport("Kernel32.dll", EntryPoint = "WTSGetActiveConsoleSessionId")]
		public static extern int WTSGetActiveConsoleSessionId();

		private WebSocket _client;
		private IMidiOutput _bmt1Output;
		private string _host = "ws://localhost:10134";
		private int _countTapForDoubleTap = 0;
		private bool _isLogEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings["DefaultEnableLogging"]);
		private bool _isHoldModeEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings["DefaultEnableHoldMode"]);
		private int _holdModeTimerDuration = Convert.ToInt32(ConfigurationManager.AppSettings["DefaultHoldModeTimerDuration"]);
		private double _wheelSimFactor = Convert.ToDouble(ConfigurationManager.AppSettings["DefaultWheelSimFactor"]);
		private double _factorBrowseNoRatchet = Convert.ToDouble(ConfigurationManager.AppSettings["DefaultFactorBrowseNoRatchet"]);
		private double _doubleTapTimerDuration = Convert.ToDouble(ConfigurationManager.AppSettings["DefaultDoubleTapTimerDuration"]);

		private Timer _turnSpeedTimer;
		private int _turnSpeedThresholdNoRatchet = Convert.ToInt32(ConfigurationManager.AppSettings["DefaultTurnSpeedTimerDurationNoRatchet"]);
		private int _turnSpeedThresholdRatchet = Convert.ToInt32(ConfigurationManager.AppSettings["DefaultTurnSpeedTimerDurationRatchet"]);

		private int _turnSpeedRatchet = 0;
		private int _turnSpeedNoRatchet = 0;
		//private List<CrownRootObject> _crownObjectList = new List<CrownRootObject>();

		public event EventHandler OnFastSpeedThresholdReached;
		public event EventHandler OnSlowSpeedThresholdReached;


		public void ToolChange(string contextName)
		{
			try
			{
				var toolChangeObject = new ToolChangeObject
				{
					message_type = "tool_change",
					session_id = _sessionId,
					tool_id = contextName
				};

				var s = JsonConvert.SerializeObject(toolChangeObject);
				_client.Send(s);
				WritelineInLogTextbox("send tool change message: " + s);
				_currentTool = contextName;

			}
			catch (Exception ex)
			{
				string err = ex.Message;
				MessageBox.Show(err);
			}
		}


		[DllImport("user32.dll")]
		public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

		public new void Scroll(int data)
		{
			// this will cause a vertical scroll
			mouse_event(0x0800, 0, 0, data, 0);
		}

		private byte CalcMidiOffsetFromCenter(int value)
		{
			const int center = 64;
			var result = center + value;
			return Convert.ToByte(result);
		}

		private string _currentTool = "";
		public void updateUIWithDeserializedData(CrownRootObject crownRootObject)
		{
			switch (crownRootObject.message_type)
			{
				case "deactivate_plugin":
					return;
				case "register_ack":
					_sessionId = crownRootObject.session_id;
					ToolChange("TabControl");
					return;
				default:
					WritelineInLogTextbox($"message_type is {crownRootObject.message_type}");
					try
					{
						_timer?.Stop();
						_timer?.Close();
						_timer?.Dispose();
						if (crownRootObject.message_type == "crown_touch_event")
						{
							if (crownRootObject.touch_state == 1)
							{
								if (_countTapForDoubleTap == 0)
									InitDetectDoubleTap();
								_countTapForDoubleTap++;
								if (_countTapForDoubleTap == 2)
									OnOnDoubleTap();
								_timer = new System.Timers.Timer(_holdModeTimerDuration) { Enabled = true };
								_timer.Start();
								//_timer.Elapsed += (sender, args) => ToolChange(_currentTool == "TabControl" ? "ProgressBar" : "TabControl");
								_timer.Elapsed += (sender, args) =>
								{
									if (_currentTool != "NumericUpDown")
										if (_isHoldModeEnabled)
											ToolChange(_currentTool == "TabControl" ? "ProgressBar" : "TabControl");
										else
											ToolChange("ProgressBar");
									_timer.Stop();
									_timer.Close();
									_timer.Dispose();
								};
							}
							else
							{
								if (_currentTool != "NumericUpDown")
									if (!_isHoldModeEnabled)
										ToolChange("TabControl");
								RestoreDetectTurnSpeed();
							}

						}
						else if (crownRootObject.message_type == "crown_turn_event")
						{
							// received a crown turn event from Craft crown
							Trace.Write("++ crown ratchet delta :" + crownRootObject.ratchet_delta + " slot delta = " + crownRootObject.delta + "\n");

							switch (crownRootObject.task_options.current_tool)
							{
								case "TabControl":
									var command = JsonConvert.SerializeObject(new AbletonCommand() { command = "scroll", direction = crownRootObject.ratchet_delta > 0 ? 1 : 0 });
									//WritelineInLogTextbox($"Send to ableton: {command}");
									WritelineInLogTextbox($"send midi to BMT 1 with Ratchet");
									_bmt1Output.Send(new byte[] { MidiEvent.CC, 0x00, CalcMidiOffsetFromCenter(crownRootObject.ratchet_delta) }, 0, 2, 0);
									if (!IsDetectTimerEnabled)
										InitDetectTurnSpeed();
									_turnSpeedRatchet += crownRootObject.ratchet_delta;
									_turnSpeedNoRatchet += crownRootObject.delta;


									break;

								case "ProgressBar":
									WritelineInLogTextbox($"send midi to BMT 1 without Ratchet");
									var deltaRounded = 0;
									deltaRounded = crownRootObject.delta > 0
										? Convert.ToInt32(Math.Ceiling(crownRootObject.delta / _factorBrowseNoRatchet))
										: -Convert.ToInt32(Math.Ceiling(Math.Abs(crownRootObject.delta) / _factorBrowseNoRatchet));
									_bmt1Output.Send(new byte[] { MidiEvent.CC, 0x00, CalcMidiOffsetFromCenter(deltaRounded) }, 0, 2, 0);

									if (!IsDetectTimerEnabled)
										InitDetectTurnSpeed();
									_turnSpeedRatchet += crownRootObject.ratchet_delta;
									_turnSpeedNoRatchet += crownRootObject.delta;

									break;
								case "NumericUpDown":
									//var factorWheel = 0;
									//factorWheel = crownRootObject.delta > 0
									//	? Convert.ToInt32(Math.Ceiling(crownRootObject.delta / _wheelSimFactor))
									//	: -Convert.ToInt32(Math.Ceiling(Math.Abs(crownRootObject.delta) / _wheelSimFactor));
									//var deltaWheel = factorWheel * Mhousewheelunit;

									var deltaWheel = crownRootObject.delta > 0
										? Convert.ToInt32(Math.Ceiling(crownRootObject.delta * _wheelSimFactor))
										: -Convert.ToInt32(Math.Ceiling(Math.Abs(crownRootObject.delta) * _wheelSimFactor));

									//WritelineInLogTextbox($"Mouse wheel {factorWheel} -- {deltaWheel}");
									Scroll(deltaWheel);
									break;

								default:
									break;
							}
						}

					}
					catch (Exception ex)
					{
						string str = ex.Message;

						MessageBox.Show(str);
					}

					break;
			}
		}

		private Timer _timer;
		private Timer _timerDoubleTap;
		private int _turnSpeedInterval = Convert.ToInt32(ConfigurationManager.AppSettings["DefaultTurnSpeedInterval"]);
		private int _slowSpeedThresholdNoRatchet = Convert.ToInt32(ConfigurationManager.AppSettings["DefaultSlowSpeedThresholdNoRatchet"]);

		public event EventHandler OnDoubleTap;

		public void SetupUIRefreshTimer()
		{

			//System.Timers.Timer timer = new System.Timers.Timer(70);
			//timer.Enabled = true;
			//timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
			//timer.Start();

			// reconnection watch dog 
			System.Timers.Timer reconnection_timer = new System.Timers.Timer(30000);
			reconnection_timer.Enabled = true;
			reconnection_timer.Elapsed += new System.Timers.ElapsedEventHandler(connection_watchdog_timer);
		}

		public void connection_watchdog_timer(object sender, System.Timers.ElapsedEventArgs e)
		{
			if (_client.IsAlive) return;
			_client = null;
			connectWithManager();

		}


		//public void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
		//{
		//	try
		//	{

		//		int totalDeltaValue = 0;
		//		int totalRatchetDeltaValue = 0;
		//		if (_crownObjectList == null || _crownObjectList.Count == 0)
		//		{
		//			//Trace.Write("Queue is empty\n");
		//			return;
		//		}
		//		else
		//		{
		//			//Trace.Write("Queue size is: " + crownObjectList.Count + "\n");
		//		}

		//		string currentToolOption = _crownObjectList[0].task_options.current_tool_option;

		//		//Trace.Write("currentToolOption is: " + currentToolOption + "\n");
		//		CrownRootObject crownRootObject = _crownObjectList[0];
		//		int count = 0;
		//		for (int i = 0; i < _crownObjectList.Count; i++)
		//		{
		//			if (currentToolOption == _crownObjectList[i].task_options.current_tool_option)
		//			{
		//				totalDeltaValue = totalDeltaValue + _crownObjectList[i].delta;
		//				totalRatchetDeltaValue = totalRatchetDeltaValue + _crownObjectList[i].ratchet_delta;
		//			}
		//			else
		//				break;

		//			count++;
		//		}

		//		if (_crownObjectList.Count >= 1)
		//		{
		//			_crownObjectList.Clear();

		//			crownRootObject.delta = totalDeltaValue;
		//			crownRootObject.ratchet_delta = totalRatchetDeltaValue;
		//			//Trace.Write("Ratchet delta is :" + totalRatchetDeltaValue + "\n");
		//			updateUIWithDeserializedData(crownRootObject);
		//		}
		//	}
		//	catch (Exception ex)
		//	{
		//		string str = ex.Message;
		//		MessageBox.Show(str);
		//	}
		//}

		//private delegate void EventHandle();

		//public void wrapperUpdateUI(string msg)
		//{
		//	Trace.Write("msg :" + msg + "\n");
		//	CrownRootObject crownRootObject = JsonConvert.DeserializeObject<CrownRootObject>(msg);

		//	if ((crownRootObject.message_type == "crown_turn_event"))
		//	{
		//		_crownObjectList.Add(crownRootObject);
		//		Trace.Write("**** UI crown ratchet delta :" + crownRootObject.ratchet_delta + " slot delta = " + crownRootObject.delta + "\n");


		//	}
		//	else if (crownRootObject.message_type == "register_ack")
		//	{
		//		// save the session id as this is used for any communication with Logi Options 
		//		_sessionId = crownRootObject.session_id;
		//		//toolChange("nothing");
		//		_lastcontext = "";

		//		if (_sendContextChange)
		//		{
		//			//sendContextChange = false;
		//			ToolChange("ProgressBar");
		//		}
		//		else
		//		{

		//			ToolChange("ProgressBar");
		//		}

		//	}
		//}

		public void openUI(string msg)
		{
			string str = msg;
			WritelineInLogTextbox(str);
		}

		public void closeConnection()
		{

		}


		public void displayError(string msg)
		{
			string str = msg;
			WritelineInLogTextbox(str);
		}


		public async Task ConnectToAbletonAsync()
		{

			//_abletonClient = new WatsonWebsocket.WatsonWsClient("localhost", 9009, false);
			//_abletonClient = new WebSocket("ws://localhost:9009");
			//_abletonServer = new WatsonWsServer();

			//_abletonServer.ClientConnected += (o, s) =>
			//{
			//	WritelineInLogTextbox("Ableton is connected");
			//};
			//_abletonServer.MessageReceived += (o, s) => WritelineInLogTextbox($"Message from ableton: {s}");
			//_abletonServer.ClientConnected += (sender, args) =>
			//{
			//	WritelineInLogTextbox($"Ableton is connected on {args.IpPort}");
			//	if(_abletonServer.ListClients().Count()>1)
			//		_abletonServer.DisconnectClient(args.IpPort);
			//};
			//_abletonServer.ClientDisconnected += (sender, args) => 
			//	WritelineInLogTextbox($"Client on {args.IpPort} is disconnected");

			//	_abletonServer.Start();



			var access = MidiAccessManager.Default;
			_bmt1Output = await access.OpenOutputAsync(access.Outputs.First(x => x.Name == "BMT 1").Id);
			//_bmt1Input = await access.OpenInputAsync(access.Inputs.First(x => x.Name == "BMT 1").Id);
			WritelineInLogTextbox($"Connected to BMT 1");
		}

		public void connectWithManager()
		{
			try
			{
				_client = new WebSocketSharp.WebSocket(_host);

				_client.OnOpen += (ss, ee) =>
				{
					openUI(string.Format("Connected to {0} successfully", _host));
				};
				_client.OnError += (ss, ee) =>
					displayError("Error: " + ee.Message);

				_client.OnMessage += (ss, ee) =>
				{
					WritelineInLogTextbox($"messag {ee.Data} end messag");
					var crownRootObject = JsonConvert.DeserializeObject<CrownRootObject>(ee.Data);
					updateUIWithDeserializedData(crownRootObject);
					//wrapperUpdateUI(ee.Data);

				};

				_client.OnClose += (ss, ee) =>
					closeConnection();

				WritelineInLogTextbox("Connection " + _client);
				_client.Connect();

				Process abletonProcess = Process.GetProcessesByName("Ableton Live 11 Beta")[0];

				CrownRegisterRootObject registerRootObject = new CrownRegisterRootObject();
				registerRootObject.message_type = "register";
				registerRootObject.plugin_guid = "41704de9-fa75-4b77-ba44-665bc9a2f8aa";
				registerRootObject.execName = "Ableton Live 11 Beta.exe";
				registerRootObject.PID = Convert.ToInt32(abletonProcess.Id);
				string s = JsonConvert.SerializeObject(registerRootObject);


				// only connect to active session process
				registerRootObject.PID = Convert.ToInt32(abletonProcess.Id);
				int activeConsoleSessionId = WTSGetActiveConsoleSessionId();
				int currentProcessSessionId = Process.GetCurrentProcess().SessionId;

				// if we are running in active session?
				if (currentProcessSessionId == activeConsoleSessionId)
				{
					WritelineInLogTextbox("Connection " + s);
					_client.Send(s);
				}
				else
				{
					Trace.TraceInformation("Inactive user session. Skipping connect");
				}
			}
			catch (Exception ex)
			{
				string str = ex.Message;
				WritelineInLogTextbox(str);
				MessageBox.Show("Ableton is not started yet");
			}
		}

		public async void Init()
		{
			try
			{
				// setup timers 
				SetupUIRefreshTimer();

				// setup connnection 
				connectWithManager();
				await ConnectToAbletonAsync();

				OnDoubleTap += (sender, args) => ToolChange(_currentTool == "NumericUpDown" ? "TabControl" : "NumericUpDown");
				OnFastSpeedThresholdReached += (sender, args) => ToolChange(("ProgressBar"));
				OnSlowSpeedThresholdReached += (sender, args) => ToolChange(("TabControl"));
				InitFields();
			}
			catch (Exception ex)
			{
				string str = ex.Message;
				MessageBox.Show(str);
			}

		}

		private void InitFields()
		{
			CheckboxHoldMode.Checked = _isHoldModeEnabled;
			CheckboxLogging.Checked = _isLogEnabled;
			TextboxTimerDuration.Text = _holdModeTimerDuration.ToString();
			TextboxWheelFactor.Text = _wheelSimFactor.ToString(CultureInfo.InvariantCulture);
		}

		public LogicraftForm()
		{
			InitializeComponent();
			WindowState = FormWindowState.Minimized;

			Init();

		}

		//for reference
		//ToolChange("ProgressBar");
		//ToolChange("NumericUpDown");
		//ToolChange("ListBox");
		//ToolChange("TextBox");
		//ToolChange("ComboBox");
		//ToolChange("CheckedListBox");
		//ToolChange("TrackBar");
		//ToolChange("TabControl");
		//ToolChange("RichTextBox");

		public void ReportToolOptionDataValueChange(string tool, string toolOption, string value)
		{
			ToolUpdateRootObject toolUpdateRootObject = new ToolUpdateRootObject
			{
				tool_id = tool,
				message_type = "tool_update",
				session_id = _sessionId,
				show_overlay = "true",
				tool_options = new List<ToolOption> { new ToolOption { name = toolOption, value = value } }
			};

			string s = JsonConvert.SerializeObject(toolUpdateRootObject);
			_client.Send(s);

			Trace.TraceInformation("MyWebSocket.ReportToolOptionDataValueChange - Tool:{0}, Tool option:{1}, Value:{2} ", tool, toolOption, value);
		}

		public void WritelineInLogTextbox(string text)
		{
			if (!_isLogEnabled) return;
			logBox.AppendText(text + '\n');
			logBox.ScrollToCaret();
		}

		private void CheckboxLogging_CheckedChanged(object sender, EventArgs e) => _isLogEnabled = CheckboxLogging.Checked;

		private void ButtonReconnect_Click(object sender, EventArgs e) => Init();

		private void CheckboxHoldMode_CheckedChanged(object sender, EventArgs e) => _isHoldModeEnabled = CheckboxHoldMode.Checked;

		private void TextboxTimerDuration_TextChanged(object sender, EventArgs e) => _holdModeTimerDuration = Convert.ToInt32(TextboxTimerDuration.Text);

		private bool IsDetectTimerEnabled => _turnSpeedTimer != null && _turnSpeedTimer.Enabled;

		private void InitDetectTurnSpeed()
		{
			_turnSpeedTimer = new Timer(_turnSpeedInterval);
			_turnSpeedTimer.Elapsed += (sender, args) =>
			{
				if (_turnSpeedRatchet > _turnSpeedThresholdRatchet)
					OnOnFastSpeedThresholdReached();
				else if (_turnSpeedNoRatchet < _slowSpeedThresholdNoRatchet)
					OnOnSlowSpeedThresholdReached();
				RestoreDetectTurnSpeed();
			};
			_turnSpeedTimer.Start();
		}

		private void RestoreDetectTurnSpeed()
		{
			_turnSpeedRatchet = 0;
			_turnSpeedNoRatchet = 0;
			_turnSpeedTimer.Stop();
			_turnSpeedTimer.Close();
			_turnSpeedTimer.Dispose();

		}

		private void InitDetectDoubleTap()
		{
			_timerDoubleTap = new Timer(_doubleTapTimerDuration) { Enabled = true };
			_timerDoubleTap.Elapsed += (sender, args) =>
			 {
				 _countTapForDoubleTap = 0;
				 _timerDoubleTap.Stop();
				 _timerDoubleTap.Close();
				 _timerDoubleTap.Dispose();
			 };
			_timerDoubleTap.Start();
		}

		protected virtual void OnOnDoubleTap()
		{
			WritelineInLogTextbox("double tap detected");
			_countTapForDoubleTap = 0;
			_timerDoubleTap.Stop();
			_timerDoubleTap.Close();
			_timerDoubleTap.Dispose();
			OnDoubleTap?.Invoke(this, EventArgs.Empty);
		}

		private void TextboxWheelFactor_TextChanged(object sender, EventArgs e)
		{
			try
			{
				_wheelSimFactor = Convert.ToDouble(TextboxWheelFactor.Text);
			}
			catch (Exception)
			{
				// ignored
			}
		}

		protected virtual void OnOnFastSpeedThresholdReached()
		{
			WritelineInLogTextbox("Fast Threshold Reached");
			OnFastSpeedThresholdReached?.Invoke(this, EventArgs.Empty);
		}

		protected virtual void OnOnSlowSpeedThresholdReached()
		{
			WritelineInLogTextbox("Slow Threshold Reached");
			OnSlowSpeedThresholdReached?.Invoke(this, EventArgs.Empty);
		}
	}

}
