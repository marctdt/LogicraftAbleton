using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;
using Commons.Music.Midi;
using Gma.System.MouseKeyHook;
using LogicraftAbleton.Helpers;
using LogicraftAbleton.Model;
using Newtonsoft.Json;
using WebSocketSharp;
using Timer = System.Timers.Timer;

namespace LogicraftAbleton
{
	public partial class LogicraftForm : Form
	{
		public enum CrownModEnum
		{
			TabControl,
			ProgressBar,
			NumericUpDown,
			TextBox
		}

		private const int Mhousewheelunit = 120;

		private const int CP_DISABLE_CLOSE_BUTTON = 0x200;
		private readonly InputSimulator _inputSimulator = new InputSimulator();
		private IMidiOutput _bmt1Output;

		private WebSocket _client;
		private int _countTapForDoubleTap;

		private CrownModEnum _currentTool = CrownModEnum.TabControl;

		private readonly double _doubleTapTimerDuration =
			Convert.ToDouble(ConfigurationManager.AppSettings["DefaultDoubleTapTimerDuration"]);

		private readonly double _factorBrowseNoRatchet =
			Convert.ToDouble(ConfigurationManager.AppSettings["DefaultFactorBrowseNoRatchet"]);

		private int _holdModeTimerDuration =
			Convert.ToInt32(ConfigurationManager.AppSettings["DefaultHoldModeTimerDuration"]);

		private readonly string _host = "ws://localhost:10134";
		private bool _isHoldModeEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings["DefaultEnableHoldMode"]);

		private bool _isKeyboardModeRatchetEnabled =
			Convert.ToBoolean(ConfigurationManager.AppSettings["DefaultIsKeyboardModeRatchetEnabled"]);

		private bool _isLogEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings["DefaultEnableLogging"]);
		private string _sessionId = "";

		private readonly int _slowSpeedThresholdNoRatchet =
			Convert.ToInt32(ConfigurationManager.AppSettings["DefaultSlowSpeedThresholdNoRatchet"]);

		private Timer _timer;
		private Timer _timerDoubleTap;

		private readonly int _turnSpeedInterval =
			Convert.ToInt32(ConfigurationManager.AppSettings["DefaultTurnSpeedInterval"]);

		private int _turnSpeedNoRatchet;

		private int _turnSpeedRatchet;

		private int _turnSpeedThresholdNoRatchet =
			Convert.ToInt32(ConfigurationManager.AppSettings["DefaultTurnSpeedThresholdNoRatchet"]);

		private readonly int _turnSpeedThresholdRatchet =
			Convert.ToInt32(ConfigurationManager.AppSettings["DefaultTurnSpeedThresholdRatchet"]);

		private bool _isDoubleTapEnable = Convert.ToBoolean(ConfigurationManager.AppSettings["DefaultDoubleTapEnable"]);

		private bool IsShortcutEnable
		{
			get => _isShortcutEnable;
			set
			{
				_isShortcutEnable = value;
				if(_isShortcutEnable)InitKeyboardInput();
				else DeactivateKeyboardInput();
			}
		}

		public string MidiPortName { get; set; } = ConfigurationManager.AppSettings["DefaultMidiPortName"];
		private Timer _turnSpeedTimer;
		private double _wheelSimFactor = Convert.ToDouble(ConfigurationManager.AppSettings["DefaultWheelSimFactor"]);

		public LogicraftForm()
		{
			InitializeComponent();

			Init();
		}

		protected override CreateParams CreateParams
		{
			get
			{
				var cp = base.CreateParams;
				cp.ClassStyle = cp.ClassStyle | CP_DISABLE_CLOSE_BUTTON;
				return cp;
			}
		}

		private bool IsDetectTimerEnabled => _turnSpeedTimer != null && _turnSpeedTimer.Enabled;

		[DllImport("kernel32.dll")]
		public static extern bool ProcessIdToSessionId(uint dwProcessID, int pSessionID);

		[DllImport("Kernel32.dll", EntryPoint = "WTSGetActiveConsoleSessionId")]
		public static extern int WTSGetActiveConsoleSessionId();
		//private List<CrownRootObject> _crownObjectList = new List<CrownRootObject>();

		public event EventHandler OnFastSpeedThresholdReached;
		public event EventHandler OnSlowSpeedThresholdReached;


		public void ToolChange(CrownModEnum contextName)
		{
			try
			{
				var toolChangeObject = new ToolChangeObject
				{
					message_type = "tool_change",
					session_id = _sessionId,
					tool_id = contextName.ToString()
				};

				var s = JsonConvert.SerializeObject(toolChangeObject);
				_client.Send(s);
				WritelineInLogTextbox("send tool change message: " + s);
				_currentTool = contextName;
			}
			catch (Exception ex)
			{
				var err = ex.Message;
				WritelineInLogTextbox(ex.StackTrace);
				MessageBox.Show($@"Tool change error:{ex.Message} : {ex.StackTrace}");
			}
		}


		[DllImport("user32.dll")]
		public static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

		public new void Scroll(int data)
		{
			mouse_event(0x0800, 0, 0, data, 0);
		}

		private byte CalcMidiOffsetFromCenter(int value)
		{
			const int center = 64;
			var result = center + value;
			return Convert.ToByte(result);
		}

		public void UpdateUiWithDeserializedData(CrownRootObject crownRootObject)
		{
			switch (crownRootObject.message_type)
			{
				case "deactivate_plugin":
					return;
				case "register_ack":
					_sessionId = crownRootObject.session_id;
					ToolChange(CrownModEnum.TabControl);
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
							if (crownRootObject.touch_state == CrownRootObject.TouchStateEnum.Touch)
							{
								if (_isDoubleTapEnable)
								{
									if (_countTapForDoubleTap == 0)
										InitDetectDoubleTap();
									if (_timerDoubleTap != null && _timerDoubleTap.Enabled)
										_countTapForDoubleTap++;
									if (_countTapForDoubleTap == 2)
										OnOnDoubleTap();
								}

								_timer = new Timer(_holdModeTimerDuration) { Enabled = true };
								_timer.Start();
								//_timer.Elapsed += (sender, args) => ToolChange(_currentTool == "TabControl" ? "ProgressBar" : "TabControl");
								_timer.Elapsed += (sender, args) =>
								{
									if (_currentTool == CrownModEnum.TabControl ||
										_currentTool == CrownModEnum.ProgressBar)
										if (_isHoldModeEnabled)
											ToolChange(_currentTool == CrownModEnum.TabControl
												? CrownModEnum.ProgressBar
												: CrownModEnum.TabControl);
										else
											ToolChange(CrownModEnum.ProgressBar);
									_timer.Stop();
									_timer.Close();
									_timer.Dispose();
								};
							}
							else if (crownRootObject.touch_state == CrownRootObject.TouchStateEnum.Release)
							{
								if (_currentTool == CrownModEnum.ProgressBar)
									if (!_isHoldModeEnabled)
										ToolChange(CrownModEnum.TabControl);
								RestoreDetectTurnSpeed();
							}
						}
						else
						{
							if (crownRootObject.message_type == "crown_turn_event")
							{
								// received a crown turn event from Craft crown
								Trace.Write("++ crown ratchet delta :" + crownRootObject.ratchet_delta +
											" slot delta = " + crownRootObject.delta + "\n");


								Enum.TryParse(crownRootObject.task_options.current_tool, true,
									out CrownModEnum currentTool);
								byte midiEventType;
								byte midiChannel;
								byte midiParameter;
								byte midiValue;
								switch (currentTool)
								{
									case CrownModEnum.TabControl:
										var command = JsonConvert.SerializeObject(new AbletonCommand
										{
											command = "scroll",
											direction = crownRootObject.ratchet_delta > 0 ? 1 : 0
										});
										//WritelineInLogTextbox($"Send to ableton: {command}");
										WritelineInLogTextbox($"send midi to {MidiPortName} with Ratchet");
										midiEventType = MidiEvent.CC;
										midiChannel = 15;
										midiParameter = 0;
										midiValue = CalcMidiOffsetFromCenter(crownRootObject.ratchet_delta);

										SendMidiMessage(midiEventType, midiChannel, midiParameter, midiValue);

										if (!_isHoldModeEnabled)
										{
											if (!IsDetectTimerEnabled)
												InitDetectTurnSpeed();
											_turnSpeedRatchet += crownRootObject.ratchet_delta;
											_turnSpeedNoRatchet += crownRootObject.delta;
										}

										;

										break;

									case CrownModEnum.ProgressBar:
										WritelineInLogTextbox("send midi to {MidiPortName} without Ratchet");
										var deltaRounded = 0;
										deltaRounded = crownRootObject.delta > 0
											? Convert.ToInt32(
												Math.Ceiling(crownRootObject.delta / _factorBrowseNoRatchet))
											: -Convert.ToInt32(Math.Ceiling(Math.Abs(crownRootObject.delta) /
																			_factorBrowseNoRatchet));
										midiEventType = MidiEvent.CC;
										midiChannel = 15;
										midiParameter = 0;
										midiValue = CalcMidiOffsetFromCenter(deltaRounded);
										SendMidiMessage(midiEventType, midiChannel, midiParameter, midiValue);

										if (!_isHoldModeEnabled)
										{
											if (!IsDetectTimerEnabled)
												InitDetectTurnSpeed();
											_turnSpeedRatchet += crownRootObject.ratchet_delta;
											_turnSpeedNoRatchet += crownRootObject.delta;
										}

										;

										break;
									case CrownModEnum.NumericUpDown:
										//var factorWheel = 0;
										//factorWheel = crownRootObject.delta > 0
										//	? Convert.ToInt32(Math.Ceiling(crownRootObject.delta / _wheelSimFactor))
										//	: -Convert.ToInt32(Math.Ceiling(Math.Abs(crownRootObject.delta) / _wheelSimFactor));
										//var deltaWheel = factorWheel * Mhousewheelunit;

										var deltaWheel = crownRootObject.delta > 0
											? Convert.ToInt32(Math.Ceiling(crownRootObject.delta * _wheelSimFactor))
											: -Convert.ToInt32(Math.Ceiling(Math.Abs(crownRootObject.delta) *
																			_wheelSimFactor));

										//WritelineInLogTextbox($"Mouse wheel {factorWheel} -- {deltaWheel}");
										Scroll(deltaWheel);
										break;
									case CrownModEnum.TextBox:
										Enum.TryParse(crownRootObject.task_options.current_tool_option, true,
											out TextBoxOptions currentTextBoxToolOption);
										var deltaRoundedKeyboard = crownRootObject.delta > 0
											? Convert.ToInt32(
												Math.Ceiling(crownRootObject.delta / _factorBrowseNoRatchet))
											: -Convert.ToInt32(Math.Ceiling(Math.Abs(crownRootObject.delta) /
																			_factorBrowseNoRatchet));

										var deltaResult = _isKeyboardModeRatchetEnabled
											? crownRootObject.ratchet_delta
											: deltaRoundedKeyboard;

										if (deltaResult > 0)
											for (var i = 0; i < deltaResult; i++)
												_inputSimulator.Keyboard.KeyPress(
													currentTextBoxToolOption == TextBoxOptions.TextBoxHeight
														? VirtualKeyCode.DOWN
														: VirtualKeyCode.RIGHT);
										else if (deltaResult < 0)
											for (var i = 0; i < Math.Abs(deltaResult); i++)
												_inputSimulator.Keyboard.KeyPress(
													currentTextBoxToolOption == TextBoxOptions.TextBoxHeight
														? VirtualKeyCode.UP
														: VirtualKeyCode.LEFT);
										break;
								}
							}
						}
					}
					catch (Exception ex)
					{
						var str = ex.Message;
						WritelineInLogTextbox(ex.StackTrace);
					//	MessageBox.Show(str); 
						MessageBox.Show($@"Tool change error:{ex.Message} : {ex.StackTrace}");
					}

					break;
			}
		}

		private async void SendMidiMessage(byte midiEventType, byte midiChannel, byte midiParameter, byte midiValue)
		{
			try
			{

				_bmt1Output.Send(
					new byte[]
					{
						(byte) (midiEventType|midiChannel), midiParameter, midiValue
					}, 0, 3, 0);
			}
			catch (Exception e)
			{
				WritelineInLogTextbox($"Cannot send message to {MidiPortName}. try to reconnect...");
				await InitMidiPortConnectionAsync();
			}
		}

		public event EventHandler OnDoubleTap;

		public void SetupUIRefreshTimer()
		{
			//System.Timers.Timer timer = new System.Timers.Timer(70);
			//timer.Enabled = true;
			//timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
			//timer.Start();

			// reconnection watch dog 
			var reconnection_timer = new Timer(30000);
			reconnection_timer.Enabled = true;
			reconnection_timer.Elapsed += connection_watchdog_timer;
		}

		public void connection_watchdog_timer(object sender, ElapsedEventArgs e)
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
			var str = msg;
			WritelineInLogTextbox(str);
		}

		public void closeConnection()
		{
		}


		public void displayError(string msg)
		{
			var str = msg;
			WritelineInLogTextbox(str);
		}


		public async Task InitMidiPortConnectionAsync()
		{
			await ConnectToMidiPort(MidiPortName);
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


			
		}


		public async Task ConnectToMidiPort(string midiPortName)
		{
			try
			{
				var access = MidiAccessManager.Default;
				_bmt1Output = await access.OpenOutputAsync(access.Outputs.First(x => x.Name == midiPortName).Id);
				if(_bmt1Output.Connection == MidiPortConnectionState.Open)
					WritelineInLogTextbox($"Connected to {midiPortName}");
				else
					throw new Exception("Cannot connect to the midi port");
				//_bmt1Input = await access.OpenInputAsync(access.Inputs.First(x => x.Name == "{MidiPortName}BMT 1").Id);
			}
			catch (Exception)
			{
				//WritelineInLogTextbox(e.StackTrace);
				//MessageBox.Show("Cannot connect to midi device");
				throw new Exception("Cannot connect to midi");
			}
		}

		public void connectWithManager()
		{
			try
			{
				_client = new WebSocket(_host);

				_client.OnOpen += (ss, ee) => { openUI(string.Format("Connected to {0} successfully", _host)); };
				_client.OnError += (ss, ee) =>
					displayError("Error: " + ee.Message);

				_client.OnMessage += (ss, ee) =>
				{
					WritelineInLogTextbox($"messag {ee.Data} end messag");
					var crownRootObject = JsonConvert.DeserializeObject<CrownRootObject>(ee.Data);
					UpdateUiWithDeserializedData(crownRootObject);
					//wrapperUpdateUI(ee.Data);
				};

				_client.OnClose += (ss, ee) =>
					closeConnection();

				WritelineInLogTextbox("Connection " + _client);
				_client.Connect();

				var abletonProcess = Process.GetProcessesByName("Ableton Live 11 Suite")[0];

				var registerRootObject = new CrownRegisterRootObject();
				registerRootObject.message_type = "register";
				registerRootObject.plugin_guid = "41704de9-fa75-4b77-ba44-665bc9a2f8aa";
				registerRootObject.execName = "Ableton Live 11 Suite.exe";
				registerRootObject.PID = Convert.ToInt32(abletonProcess.Id);
				var s = JsonConvert.SerializeObject(registerRootObject);


				// only connect to active session process
				registerRootObject.PID = Convert.ToInt32(abletonProcess.Id);
				var activeConsoleSessionId = WTSGetActiveConsoleSessionId();
				var currentProcessSessionId = Process.GetCurrentProcess().SessionId;

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
				//var str = ex.Message;
				//WritelineInLogTextbox(ex.StackTrace);
				//MessageBox.Show("Ableton is not started yet");
				throw new Exception("Ableton is not started yet");
			}
		}

		public async void Init()
		{
			try
			{
				// setup timers 
				SetupUIRefreshTimer();

				//await ConnectToAbletonAsync();
				await InitMidiPortConnectionAsync();
				connectWithManager();


				OnDoubleTap += (sender, args) => ToolChange(GetNextTool());
				OnFastSpeedThresholdReached += (sender, args) => ToolChange(CrownModEnum.ProgressBar);
				OnSlowSpeedThresholdReached += (sender, args) => ToolChange(CrownModEnum.TabControl);
				InitUi();
				var closeListener = new ClosingWsListener();
				closeListener.OnCloseRequest += exitToolStripMenuItem_Click;
				InitKeyboardInput();

			}
			catch (Exception ex)
			{
				var str = ex.StackTrace;
			//	WritelineInLogTextbox(ex.StackTrace);
				MessageBox.Show($"Error: {ex.Message} : {str}");
				exitToolStripMenuItem_Click(this, null);
				throw;
			}
		}

		private IKeyboardMouseEvents _keyHook;
		private bool _isShortcutEnable = Convert.ToBoolean(ConfigurationManager.AppSettings["DefaultShortcutEnable"]);

		private void InitKeyboardInput()
		{
			DeactivateKeyboardInput();
			if (!IsShortcutEnable)
				return;

			_keyHook = Hook.GlobalEvents();
			_keyHook.KeyDown += (sender, args) =>
			{
				switch (args.KeyCode)
				{
					case Keys.OemQuotes when args.Modifiers == (Keys.Alt | Keys.Shift):
						args.SuppressKeyPress = true;
						ToolChange(CrownModEnum.TabControl);
						break;
					case Keys.Oemcomma when args.Modifiers == (Keys.Alt | Keys.Shift):
						args.SuppressKeyPress = true;
						ToolChange(CrownModEnum.NumericUpDown);
						break;
					case Keys.OemPeriod when args.Modifiers == (Keys.Alt | Keys.Shift):
						args.SuppressKeyPress = true;
						ToolChange(CrownModEnum.TextBox);
						break;
				}
			};
		}

		private void DeactivateKeyboardInput()
		{
			_keyHook?.Dispose();
			_keyHook = null;
		}

		private CrownModEnum GetNextTool()
		{
			var crowModCount = Enum.GetNames(typeof(CrownModEnum)).Length;
			var nextTool = _currentTool + 1 != CrownModEnum.ProgressBar
				? _currentTool + 1
				: CrownModEnum.ProgressBar + 1;

			if ((int)nextTool >= crowModCount)
				nextTool = 0;
			return nextTool;
		}

		private void InitUi()
		{
			CheckboxHoldMode.Checked = _isHoldModeEnabled;
			CheckboxLogging.Checked = _isLogEnabled;
			CheckboxKeyboardRatchetEnabled.Checked = _isKeyboardModeRatchetEnabled;
			CheckboxDoubleTapEnabled.Checked = _isDoubleTapEnable;
			CheckboxShortcutEnabled.Checked = IsShortcutEnable;
			TextboxTimerDuration.Text = _holdModeTimerDuration.ToString();
			TextboxWheelFactor.Text = _wheelSimFactor.ToString(CultureInfo.InvariantCulture);
			MinimizeWindow();
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
			var toolUpdateRootObject = new ToolUpdateRootObject
			{
				tool_id = tool,
				message_type = "tool_update",
				session_id = _sessionId,
				show_overlay = "true",
				tool_options = new List<ToolOption> { new ToolOption { name = toolOption, value = value } }
			};

			var s = JsonConvert.SerializeObject(toolUpdateRootObject);
			_client.Send(s);

			Trace.TraceInformation(
				"MyWebSocket.ReportToolOptionDataValueChange - Tool:{0}, Tool option:{1}, Value:{2} ", tool, toolOption,
				value);
		}

		public void WritelineInLogTextbox(string text)
		{
			if (!_isLogEnabled) return;
			logBox.AppendText(text + '\n');
			logBox.ScrollToCaret();
		}

		private void CheckboxLogging_CheckedChanged(object sender, EventArgs e)
		{
			_isLogEnabled = CheckboxLogging.Checked;
		}

		private void ButtonReconnect_Click(object sender, EventArgs e)
		{
			Init();
		}

		private void CheckboxHoldMode_CheckedChanged(object sender, EventArgs e)
		{
			_isHoldModeEnabled = CheckboxHoldMode.Checked;
		}

		private void TextboxTimerDuration_TextChanged(object sender, EventArgs e)
		{
			_holdModeTimerDuration = Convert.ToInt32(TextboxTimerDuration.Text);
		}

		private void InitDetectTurnSpeed()
		{
			_turnSpeedTimer = new Timer(_turnSpeedInterval);
			_turnSpeedTimer.Elapsed += (sender, args) =>
			{
				if (Math.Abs(_turnSpeedRatchet) > _turnSpeedThresholdRatchet)
					OnOnFastSpeedThresholdReached();
				else if (Math.Abs(_turnSpeedNoRatchet) < _slowSpeedThresholdNoRatchet)
					OnOnSlowSpeedThresholdReached();
				RestoreDetectTurnSpeed();
			};
			_turnSpeedTimer.Start();
		}

		private void RestoreDetectTurnSpeed()
		{
			_turnSpeedRatchet = 0;
			_turnSpeedNoRatchet = 0;
			_turnSpeedTimer?.Stop();
			_turnSpeedTimer?.Close();
			_turnSpeedTimer?.Dispose();
		}

		private void InitDetectDoubleTap()
		{
			if (!_isDoubleTapEnable) return;
			_timerDoubleTap = new Timer(_doubleTapTimerDuration) { Enabled = true };
			_timerDoubleTap.Elapsed += (sender, args) =>
			{
				WritelineInLogTextbox("End of Double tap timer");
				_countTapForDoubleTap = 0;
				_timerDoubleTap.Stop();
				_timerDoubleTap.Close();
				_timerDoubleTap.Dispose();
			};
			_timerDoubleTap.Start();
		}

		protected virtual void OnOnDoubleTap()
		{
			if (!_isDoubleTapEnable) return;
			WritelineInLogTextbox("double tap detected");
			_countTapForDoubleTap = 0;
			_timerDoubleTap?.Stop();
			_timerDoubleTap?.Close();
			_timerDoubleTap?.Dispose();
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

		private void CheckboxKeyboardRatchetEnabled_CheckedChanged(object sender, EventArgs e)
		{
			_isKeyboardModeRatchetEnabled = CheckboxKeyboardRatchetEnabled.Checked;
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			LogicraftNotifyTray.Visible = false;
			if (Application.MessageLoop)
				// WinForms app
				Application.Exit();
			else
				// Console app
				Environment.Exit(1);
		}

		private void LogicraftForm_Shown(object sender, EventArgs e)
		{
			MinimizeWindow();
		}


		private void MinimizeWindow()
		{
			WindowState = FormWindowState.Minimized;
			Hide();
		}

		private void RestoreWindow()
		{
			Show();
			WindowState = FormWindowState.Normal;
			BringToFront();
			Activate();
			TopMost = true;
		}

		private void LogicraftForm_Resize(object sender, EventArgs e)
		{
			if (WindowState == FormWindowState.Minimized)
				MinimizeWindow();
		}

		private void hideToolStripMenuItem_Click(object sender, EventArgs e) => MinimizeWindow();

		private void LogicraftNotifyTray_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button != MouseButtons.Left) return;
			RestoreWindow();
		}

		private void showToolStripMenuItem_Click(object sender, EventArgs e) => RestoreWindow();

		private void CheckboxDoubleTapEnabled_CheckedChanged(object sender, EventArgs e) => _isDoubleTapEnable = CheckboxDoubleTapEnabled.Checked;

		private void CheckboxShortcutEnabled_CheckedChanged(object sender, EventArgs e) => IsShortcutEnable = CheckboxShortcutEnabled.Checked;
	}
}