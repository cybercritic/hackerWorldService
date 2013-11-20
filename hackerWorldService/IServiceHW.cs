using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace hackerWorldService
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    //[ServiceContract(SessionMode = SessionMode.Required)]
    [ServiceContract]
    public interface IhackerWorldService
    {
        [OperationContract]
        string RegisterUser(string username, string password, string email);

        [OperationContract]
        string sendPassword(string email);

        [OperationContract]
        string Login(string username, string password);

        [OperationContract]
        string changePassword(string username, string password);

        [OperationContract]
        string deleteHDDItem(string sessionID, int hddSlot);

        [OperationContract]
        string deleteCPUItem(string sessionID, int cpuSlot);

        [OperationContract]
        string deleteMission(string sessionID);

        [OperationContract]
        HardDrive getHDDInfo(string sessionID);

        [OperationContract]
        CPUload getCPUInfo(string sessionID);

        [OperationContract]
        ProgramTypes getProgramTypes(string sessionID);

        [OperationContract]
        CompilerJob getCompilerJob(string sessionID);

        [OperationContract]
        UserInfo getUserInfo(string sessionID);

        [OperationContract]
        MissionList getMissionList(string sessionID);

        [OperationContract]
        UserMission getUserMission(string sessionID);

        [OperationContract]
        SlaveListHW getSlaves(string sessionID);

        [OperationContract]
        UserStats getStats(string sessionID);

        [OperationContract]
        string checkClientVersion(string version);

        [OperationContract]
        string purchaseFromMarket(string sessionID, ProgramHW software);

        [OperationContract]
        string swapProgsHDD(string sessionID, int slotA, int slotB);

        [OperationContract]
        string addCPUprogram(string sessionID, ProgramHW program, int slot);

        [OperationContract]
        string addCompilerJob(string sessionID, CompilerJob job);

        [OperationContract]
        string addHDDslot(string sessionID);

        [OperationContract]
        string addCPUslot(string sessionID);

        [OperationContract]
        string addMission(string sessionID, UserMission mission);

        [OperationContract]
        string claimCompilerJob(string sessionID);

        [OperationContract]
        string claimMission(string sessionID);

        [OperationContract]
        string claimDonation(string sessionID);

        [OperationContract]
        string setSlave(string sessionID, Slave slave);

        [OperationContract]
        string deleteSlave(string sessionID, int slaveID);

        [OperationContract]
        string decrementHDDuses(string sessionID, ProgramHW program);
    }

    [DataContract]
    public class CompilerJob
    {
        private long userID = -1;
        private int programType = -1;
        private int programSubType = -1;
        private int programVersion = -1;
        private long startTime = -1;
        private long endTime = -1;
        private long buddyID = -1;
        private string buddyName = "anon";
        private bool active = false;

        [DataMember]
        public long UserID { get { return userID; } set { userID = value; } }
        [DataMember]
        public int ProgramType { get { return programType; } set { programType = value; } }
        [DataMember]
        public int ProgramSubType { get { return programSubType; } set { programSubType = value; } }
        [DataMember]
        public int ProgramVersion { get { return programVersion; } set { programVersion = value; } }
        [DataMember]
        public long StartTime { get { return startTime; } set { startTime = value; } }
        [DataMember]
        public long EndTime { get { return endTime; } set { endTime = value; } }
        [DataMember]
        public long BuddyID { get { return buddyID; } set { buddyID = value; } }
        [DataMember]
        public string BuddyName { get { return buddyName; } set { buddyName = value; } }
        [DataMember]
        public bool Active { get { return active; } set { active = value; } }
    }

    [DataContract]
    public class ProgramTypes
    {
        private List<ProgramHW> programTypes = new List<ProgramHW>();

        [DataMember]
        public List<ProgramHW> ProgramTypesLst { get { return programTypes; } set { programTypes = value; } }
    }

    [DataContract]
    public class UserInfo
    {
        private int hddSlots = -1;
        private int cpuSlots = -1;
        private long userCash = -1;
        private long userGold = -1;
        private long userHatPoints = -1;
        
        [DataMember]
        public int HddSlots { get { return hddSlots; } set { hddSlots = value; } }
        [DataMember]
        public int CpuSlots { get { return cpuSlots; } set { cpuSlots = value; } }
        [DataMember]
        public long UserCash { get { return userCash; } set { userCash = value; } }
        [DataMember]
        public long UserGold { get { return userGold; } set { userGold = value; } }
        [DataMember]
        public long UserHatPoints { get { return userHatPoints; } set { userHatPoints = value; } }
    }

    [DataContract]
    public class UserMission
    {
        private long userID = 0;
        private int missionID = 0;
        private int passStrength = 0;
        private int programGroup = 0;
        private int programSubGroup = 0;
        private int programVersion = 0;
        private bool needAdmin = false;
        private bool havePass = false;
        private bool haveAdmin = false;

        [DataMember]
        public long UserID { get { return userID; } set { userID = value; } }
        [DataMember]
        public int MissionID { get { return missionID; } set { missionID = value; } }
        [DataMember]
        public int PassStrength { get { return passStrength; } set { passStrength = value; } }
        [DataMember]
        public int ProgramGroup { get { return programGroup; } set { programGroup = value; } }
        [DataMember]
        public int ProgramSubGroup { get { return programSubGroup; } set { programSubGroup = value; } }
        [DataMember]
        public int ProgramVersion { get { return programVersion; } set { programVersion = value; } }
        [DataMember]
        public bool NeedAdmin { get { return needAdmin; } set { needAdmin = value; } }
        [DataMember]
        public bool HavePass { get { return havePass; } set { havePass = value; } }
        [DataMember]
        public bool HaveAdmin { get { return haveAdmin; } set { haveAdmin = value; } }
    }

    [DataContract]
    public class SlaveListHW
    {
        private List<Slave> slaveList = new List<Slave>();

        [DataMember]
        public List<Slave> SlaveList { get { return slaveList; } set { slaveList = value; } }
    }

    [DataContract]
    public class Slave
    {
        private long userID = 0;
        private long endTime = 0;
        private int slaveID = 0;
        private bool userPass = false;
        private bool adminPass = false;
        private List<SlaveSlot> slaveFiles = new List<SlaveSlot>();

        [DataMember]
        public long UserID { get { return userID; } set { userID = value; } }
        [DataMember]
        public long EndTime { get { return endTime; } set { endTime = value; } }
        [DataMember]
        public int SlaveID { get { return slaveID; } set { slaveID = value; } }
        [DataMember]
        public bool UserPass { get { return userPass; } set { userPass = value; } }
        [DataMember]
        public bool AdminPass { get { return adminPass; } set { adminPass = value; } }
        [DataMember]
        public List<SlaveSlot> SlaveFiles { get { return slaveFiles; } set { slaveFiles = value; } }
    }

    [DataContract]
    public class SlaveSlot
    {
        private long userID = 0;
        private int slaveID = 0;
        private int slotID = 0;
        private int programGroup = 0;
        private int programSubGroup = 0;
        private int programVersion = 0;

        [DataMember]
        public long UserID { get { return userID; } set { userID = value; } }
        [DataMember]
        public int SlaveID { get { return slaveID; } set { slaveID = value; } }
        [DataMember]
        public int SlotID { get { return slotID; } set { slotID = value; } }
        [DataMember]
        public int ProgramGroup { get { return programGroup; } set { programGroup = value; } }
        [DataMember]
        public int ProgramSubGroup { get { return programSubGroup; } set { programSubGroup = value; } }
        [DataMember]
        public int ProgramVersion { get { return programVersion; } set { programVersion = value; } }
    }

    [DataContract]
    public class MissionType
    {
        private int missionID = 0;
        private int hatPoints = 0;
        private int missionPay = 0;
        private string description = "";

        [DataMember]
        public int MissionID { get { return missionID; } set { missionID = value; } }
        [DataMember]
        public int HatPoints { get { return hatPoints; } set { hatPoints = value; } }
        [DataMember]
        public int MissionPay { get { return missionPay; } set { missionPay = value; } }
        [DataMember]
        public string Description { get { return description; } set { description = value; } }
    }

    [DataContract]
    public class MissionList
    {
        private List<MissionType> missions = new List<MissionType>();

        [DataMember]
        public List<MissionType> Missions { get { return missions; } set { missions = value; } }
    }

    [DataContract]
    public class HardDrive
    {
        private int driveSize = 8;
        private List<ProgramHW> programs = new List<ProgramHW>();

        [DataMember]
        public int DriveSize { get { return driveSize; } set { driveSize = value; } }
        [DataMember]
        public List<ProgramHW> Programs { get { return programs; } set { programs = value; } }
    }

    [DataContract]
    public class CPUload
    {
        private int totalCPUslots = 2;
        private List<CPUslotHW> programs = new List<CPUslotHW>();

        [DataMember]
        public int TotalCPUslots { get { return totalCPUslots; } set { totalCPUslots = value; } }
        [DataMember]
        public List<CPUslotHW> Programs { get { return programs; } set { programs = value; } }
    }

    [DataContract]
    public class CPUslotHW
    {
        private int cpuSlot = -1;
        private int programType = -1;
        private int programSubType = -1;
        private int programVersion = -1;

        [DataMember]
        public int CpuSlot { get { return cpuSlot; } set { cpuSlot = value; } }
        [DataMember]
        public int ProgramType { get { return programType; } set { programType = value; } }
        [DataMember]
        public int ProgramSubType { get { return programSubType; } set { programSubType = value; } }
        [DataMember]
        public int ProgramVersion { get { return programVersion; } set { programVersion = value; } }
    }

    [DataContract]
    public class ProgramHW
    {
        private int programType = -1;
        private int programSubType = -1;
        private int hddSlot = -1;
        private int usesLeft = 5;
        private int programVersion = 1;
        private string programName = "";
        private string programDescription = "";
        private float basePrice = 0;

        [DataMember]
        public int ProgramType { get { return programType; } set { programType = value; } }
        [DataMember]
        public int HddSlot { get { return hddSlot; } set { hddSlot = value; } }
        [DataMember]
        public int ProgramVersion { get { return programVersion; } set { programVersion = value; } }
        [DataMember]
        public int ProgramSubType { get { return programSubType; } set { programSubType = value; } }
        [DataMember]
        public string ProgramDescription { get { return programDescription; } set { programDescription = value; } }
        [DataMember]
        public string ProgramName { get { return programName; } set { programName = value; } }
        [DataMember]
        public int UsesLeft { get { return usesLeft; } set { usesLeft = value; } }
        [DataMember]
        public float BasePrice { get { return basePrice; } set { basePrice = value; } }
    }

    [DataContract]
    public class UserStats
    {
        private List<UserStatOne> top10black = new List<UserStatOne>();
        private List<UserStatOne> top10white = new List<UserStatOne>();
        private List<UserStatOne> top10cash = new List<UserStatOne>();
        private long registeredUsers = 0;
        
        [DataMember]
        public List<UserStatOne> Top10black { get { return top10black; } set { top10black = value; } }
        [DataMember]
        public List<UserStatOne> Top10white { get { return top10white; } set { top10white = value; } }
        [DataMember]
        public List<UserStatOne> Top10cash { get { return top10cash; } set { top10cash = value; } }
        [DataMember]
        public long RegisteredUsers { get { return registeredUsers; } set { registeredUsers = value; } }
    }

    [DataContract]
    public class UserStatOne
    {
        private string userName = null;
        private long amount = 0;

        [DataMember]
        public string UserName { get { return userName; } set { userName = value; } }
        [DataMember]
        public long Amount { get { return amount; } set { amount = value; } }
    }
}
