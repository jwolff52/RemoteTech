using RemoteTech.SimpleTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace RemoteTech.AddOns
{
    /// <summary>
    /// This class connects to the KSP-Addon KerbalAlarmClock, created by triggerAU
    /// Topic: http://forum.kerbalspaceprogram.com/threads/24786
    /// </summary>
    public class KerbalAlarmClockAddon : AddOn
    {
        private bool KaCApiReady = false;

        protected static Type KACAlarmType;

        public enum AlarmTypeEnum
        {
            Raw,
            Maneuver,
            ManeuverAuto,
            Apoapsis,
            Periapsis,
            AscendingNode,
            DescendingNode,
            LaunchRendevous,
            Closest,
            SOIChange,
            SOIChangeAuto,
            Transfer,
            TransferModelled,
            Distance,
            Crew,
            EarthTime,
            Contract,
            ContractAuto
        }

        public KerbalAlarmClockAddon()
            : base("KerbalAlarmClock", "KerbalAlarmClock.KerbalAlarmClock")
        {
            // change the bindings
            this.bFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.InvokeMethod;
            if (this.assemblyLoaded)
            {
                this.loadInstance();
            }

            var loadedAssembly = AssemblyLoader.loadedAssemblies.GetTypeByName(this.assemblyType, "KerbalAlarmClock.KACAlarm");
            if (loadedAssembly != null)
            {
                RTLog.Notify("Successfull connected to Assembly {0}", RTLogLevel.Assembly, "KerbalAlarmClock");
                KACAlarmType = loadedAssembly.Assembly.GetType();
            }

            refreshAlarmList();

            if (actualAlarms != null)
            {
                RTLog.Notify("actualAlarms typeof {0}", actualAlarms.GetType());
            }
            else
            {
                RTLog.Notify("Grrrrrr");
            }
        }

        private void refreshAlarmList()
        {
            actualAlarms = assemblyType.GetField("alarms", BindingFlags.Public | BindingFlags.Static);
        }

        private void loadInstance()
        {
            /// load KaC instance
            try
            {
                this.instance = this.assemblyType.GetField("APIInstance", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                this.KaCApiReady = (bool)this.assemblyType.GetField("APIReady", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            }
            catch (Exception ex)
            {
                RTLog.Verbose("AddOn.loadInstance exception: {0}", RTLogLevel.Assembly, ex);
            }
        }

        /// <summary>
        /// Returns the KaC API Status
        /// </summary>
        public bool APIReady()
        {
            return this.KaCApiReady;
        }
        
        /// <summary>
        /// Create a new Alarm
        /// </summary>
        /// <param name="AlarmType">What type of alarm are we creating</param>
        /// <param name="Name">Name of the Alarm for the display</param>
        /// <param name="UT">Universal Time for the alarm</param>
        /// <param name="VesselId">Guid of the Vessel this Alarm is for</param>
        /// <returns>ID of the newly created alarm</returns>
        public String CreateAlarm(AlarmTypeEnum AlarmType, String Name, Double UT, String VesselId)
        {
            // Is KaC Ready?
            if (!this.APIReady()) return String.Empty;

            var result = this.invoke(new System.Object[] { (Int32)AlarmType, Name, UT });
            RTLog.Notify("Got result from KACApi");
            if (result != null)
			{
                RTLog.Notify("Result is not null");
                refreshAlarmList();
                KACAlarm a = Alarms.Find(z => z.ID == result);
                if (a != null)
                {
                    RTLog.Notify("Got alarm from API");
                    a.VesselID = VesselId;
                    return (String)result;
                }
			}
            return String.Empty;
        }

        /// <summary>
        /// Delete Alarm Method for calling via API
        /// </summary>
        /// <param name="AlarmID">Unique ID of the alarm</param>
        /// <returns>Success</returns>
        public Boolean DeleteAlarm(String AlarmID)
        {
            // Is KaC Ready?
            if (!this.APIReady()) return false;

            var result = this.invoke(new System.Object[] { AlarmID });

            if (result != null) return (Boolean)result;
            return false;
        }

        private Object actualAlarms;

        /// <summary>
        /// The list of Alarms that are currently active in game
        /// </summary>
        internal KACAlarmList Alarms
        {
            get
            {
                return ExtractAlarmList(actualAlarms);
            }
        }

        /// <summary>
        /// This converts the KACAlarmList actual object to a new List for consumption
        /// </summary>
        /// <param name="actualAlarmList"></param>
        /// <returns></returns>
        private KACAlarmList ExtractAlarmList(Object actualAlarmList)
        {
            KACAlarmList ListToReturn = new KACAlarmList();
            try
            {
                //iterate each "value" in the dictionary

                foreach (var item in (IList)actualAlarmList)
                {
                    KACAlarm r1 = new KACAlarm(item);
                    RTLog.Notify("It's an alarm, adding it.");
                    ListToReturn.Add(r1);
                }
            }
            catch (Exception ex)
            {
                RTLog.Notify("Arrggg: {0}", ex.Message);
                RTLog.Notify("actualAlarmList typeof {0}", actualAlarmList.GetType());
                //throw ex;
                //
            }
            return ListToReturn;
        }

        public class KACAlarm
            {
                internal KACAlarm(Object a)
                {
                    actualAlarm = a;
                    VesselIDField = KACAlarmType.GetField("VesselID");
                    IDField = KACAlarmType.GetField("ID");
                    NameField = KACAlarmType.GetField("Name");
                    NotesField = KACAlarmType.GetField("Notes");
                    AlarmTypeField = KACAlarmType.GetField("TypeOfAlarm");
                    AlarmTimeProperty = KACAlarmType.GetProperty("AlarmTimeUT");
                    AlarmMarginField = KACAlarmType.GetField("AlarmMarginSecs");
                    AlarmActionField = KACAlarmType.GetField("AlarmAction");
                    RemainingField = KACAlarmType.GetField("Remaining");

                    XferOriginBodyNameField = KACAlarmType.GetField("XferOriginBodyName");
                    //LogFormatted("XFEROrigin:{0}", XferOriginBodyNameField == null);
                    XferTargetBodyNameField = KACAlarmType.GetField("XferTargetBodyName");

                    RepeatAlarmField = KACAlarmType.GetField("RepeatAlarm");
                    RepeatAlarmPeriodProperty = KACAlarmType.GetProperty("RepeatAlarmPeriodUT");

                    //PropertyInfo[] pis = KACAlarmType.GetProperties();
                    //foreach (PropertyInfo pi in pis)
                    //{
                    //    LogFormatted("P:{0}-{1}", pi.Name, pi.DeclaringType);
                    //}
                    //FieldInfo[] fis = KACAlarmType.GetFields();
                    //foreach (FieldInfo fi in fis)
                    //{
                    //    LogFormatted("F:{0}-{1}", fi.Name, fi.DeclaringType);
                    //}
                }

                private Object actualAlarm;

                private FieldInfo VesselIDField;
                /// <summary>
                /// Unique Identifier of the Vessel that the alarm is attached to
                /// </summary>
                public String VesselID
                {
                    get { return (String)VesselIDField.GetValue(actualAlarm); }
                    set { VesselIDField.SetValue(actualAlarm, value); }
                }

                private FieldInfo IDField;
                /// <summary>
                /// Unique Identifier of this alarm
                /// </summary>
                public String ID
                {
                    get { return (String)IDField.GetValue(actualAlarm); }
                }

                private FieldInfo NameField;
                /// <summary>
                /// Short Text Name for the Alarm
                /// </summary>
                public String Name
                {
                    get { return (String)NameField.GetValue(actualAlarm); }
                    set { NameField.SetValue(actualAlarm, value); }
                }

                private FieldInfo NotesField;
                /// <summary>
                /// Longer Text Description for the Alarm
                /// </summary>
                public String Notes
                {
                    get { return (String)NotesField.GetValue(actualAlarm); }
                    set { NotesField.SetValue(actualAlarm, value); }
                }

                private FieldInfo XferOriginBodyNameField;
                /// <summary>
                /// Name of the origin body for a transfer
                /// </summary>
                public String XferOriginBodyName
                {
                    get { return (String)XferOriginBodyNameField.GetValue(actualAlarm); }
                    set { XferOriginBodyNameField.SetValue(actualAlarm, value); }
                }

                private FieldInfo XferTargetBodyNameField;
                /// <summary>
                /// Name of the destination body for a transfer
                /// </summary>
                public String XferTargetBodyName
                {
                    get { return (String)XferTargetBodyNameField.GetValue(actualAlarm); }
                    set { XferTargetBodyNameField.SetValue(actualAlarm, value); }
                }
                
                private FieldInfo AlarmTypeField;
                /// <summary>
                /// What type of Alarm is this - affects icon displayed and some calc options
                /// </summary>
                public AlarmTypeEnum AlarmType { get { return (AlarmTypeEnum)AlarmTypeField.GetValue(actualAlarm); } }

                private PropertyInfo AlarmTimeProperty;
                /// <summary>
                /// In game UT value of the alarm
                /// </summary>
                public Double AlarmTime
                {
                    get { return (Double)AlarmTimeProperty.GetValue(actualAlarm,null); }
                    set { AlarmTimeProperty.SetValue(actualAlarm, value, null); }
                }

                private FieldInfo AlarmMarginField;
                /// <summary>
                /// In game seconds the alarm will fire before the event it is for
                /// </summary>
                public Double AlarmMargin
                {
                    get { return (Double)AlarmMarginField.GetValue(actualAlarm); }
                    set { AlarmMarginField.SetValue(actualAlarm, value); }
                }

                private FieldInfo AlarmActionField;
                /// <summary>
                /// What should the Alarm Clock do when the alarm fires
                /// </summary>
                public AlarmActionEnum AlarmAction
                {
                    get { return (AlarmActionEnum)AlarmActionField.GetValue(actualAlarm); }
                    set { AlarmActionField.SetValue(actualAlarm, (Int32)value); }
                }

                private FieldInfo RemainingField;
                /// <summary>
                /// How much Game time is left before the alarm fires
                /// </summary>
                public Double Remaining { get { return (Double)RemainingField.GetValue(actualAlarm); } }


                private FieldInfo RepeatAlarmField;
                /// <summary>
                /// Whether the alarm will be repeated after it fires
                /// </summary>
                public Boolean RepeatAlarm
                {
                    get { return (Boolean)RepeatAlarmField.GetValue(actualAlarm); }
                    set { RepeatAlarmField.SetValue(actualAlarm, value); }
                }
                private PropertyInfo RepeatAlarmPeriodProperty;
                /// <summary>
                /// Value in Seconds after which the alarm will repeat
                /// </summary>
                public Double RepeatAlarmPeriod
                {
                    get
                    {
                        try { return (Double)RepeatAlarmPeriodProperty.GetValue(actualAlarm, null); }
                        catch (Exception) { return 0; }
                    }
                    set { RepeatAlarmPeriodProperty.SetValue(actualAlarm, value, null); }
                }

                public enum AlarmStateEventsEnum
                {
                    Created,
                    Triggered,
                    Closed,
                    Deleted,
                }
            }

        public enum AlarmActionEnum
        {
            [Description("Do Nothing-Delete When Past")]        DoNothingDeleteWhenPassed,
            [Description("Do Nothing")]                         DoNothing,
            [Description("Message Only-No Affect on warp")]     MessageOnly,
            [Description("Kill Warp Only-No Message")]          KillWarpOnly,
            [Description("Kill Warp and Message")]              KillWarp,
            [Description("Pause Game and Message")]             PauseGame,
        }

        public enum TimeEntryPrecisionEnum
        {
            Seconds = 0,
            Minutes = 1,
            Hours = 2,
            Days = 3,
            Years = 4
        }

        public class KACAlarmList : List<KACAlarm>
        {

        }
    }
}
