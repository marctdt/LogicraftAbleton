namespace LogicraftAbleton.Model
{
	public class CrownRootObject
	{
		public enum TouchStateEnum
		{
			Release,
			Touch
		}

		public string message_type { get; set; }
		public int device_id { get; set; }
		public int unit_id { get; set; }
		public int feature_id { get; set; }
		public string task_id { get; set; }
		public string session_id { get; set; }
		public TouchStateEnum touch_state { get; set; }
		public TaskOptions task_options { get; set; }
		public int delta { get; set; }
		public int ratchet_delta { get; set; }
		public long time_stamp { get; set; }
		public string state { get; set; }
	}
}