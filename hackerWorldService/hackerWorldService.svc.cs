using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.ServiceModel.Channels;
using System.Text;
using System.Net.Mail;


namespace hackerWorldService
{
    enum hex { A = 10, B, C, D, E, F }
    enum alpha { a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p, q, r, s, t, u, v, w, x, y, z }

    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    //[ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Single)]
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple , InstanceContextMode = InstanceContextMode.PerCall)]
    public class hackerWorldService : IhackerWorldService, IDisposable 
    {
        //private SessionData sessionData;
        private HardDrive userHDD;
        private CPUload userCPU;

        hackerWorldService()
        {
        }

        ~hackerWorldService()
        {
            SQLq.SQLraw.Close();
        }

        public void Dispose()
        {
            SQLq.SQLraw.Close();
            //GC.SuppressFinalize(this);
        }

        SQLqueries SQLq = new SQLqueries();

        public string RegisterUser(string username, string password, string email)
        {
            try
            {
                if (username.IndexOf(" ") != -1 || email.IndexOf(" ") != -1)
                    return "error: invalid username/email";

                return SQLq.addUser(username, password, email);
            }
            catch { return "error: exception (RegisterUser)"; }
        }

        public string FormatHDD(string sessionID)
        {
            try
            {
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return "error: invalid session data";

                return SQLq.formatHDD(userID.ToString());
            }
            catch { return "error: exception (formatHDD)"; }
        }

        public string sendPassword(string email)
        {
            try
            {
                if (!SQLq.emailExists(email))
                    return "error: email not registered";

                string username = SQLq.getUsernameFromEmail(email);
                if (username == null)
                    return "error: invalid email";

                string newPassword = makePassword();
                if (!SQLq.updatePassword(username, newPassword, false))
                    return "error: unable to set password";

                MailMessage mail = new MailMessage();
                mail.From = new MailAddress("hackerworldonline@gmail.com");
                mail.To.Add(new MailAddress(email));

                mail.Subject = "Request for new password and username for Hacker World Online";
                mail.Body = "Your username for Hacker World Online is: " + username + "\nYour new password is: " + newPassword;

                SmtpClient client = new SmtpClient("smtp.gmail.com");
                client.Port = 587;
                client.Credentials = new System.Net.NetworkCredential("hackerworldonline@gmail.com", "hacker@support");
                client.EnableSsl = true;
                client.Send(mail);

                return "email sent to " + email;
            }
            catch { return "error: exception (sendPassword)"; }
        }

        public string changePassword(string username, string password)
        {
            try
            {
                if (!SQLq.updatePassword(username, password, true))
                    return "error: unable to change password";

                return "password changed";
            }
            catch { return "error: exception (changePassword)"; }
        }

        public string Login(string username, string password)
        {
            try
            {
                if (!SQLq.passwordOK(password, username))
                    return "error: invalid username/password";

                long userID = SQLq.getUserIDFromName(username);
                if (userID < 0)
                    return "error: couldn't find user";

                DateTime sessionStart = DateTime.Now.ToUniversalTime();
                string sessionID = new Crypto().getSHA1hash(username + password + sessionStart.Ticks.ToString());

                logUserLogin(username, sessionID);

                return sessionID;
            }
            catch { return "error: exception (Login)"; }
        }

        private string makePassword()
        {
            try
            {
                string result = "";

                Random rand = new Random((int)DateTime.Now.Ticks);

                for (int i = 0; i < 8; i++)
                {
                    string tmp = ((alpha)rand.Next(26)).ToString();
                    if (rand.Next(3) == 0)
                        tmp = tmp.ToUpper();
                    result += tmp;
                }

                return result;
            }
            catch { return "error: exception (makePassword)"; }
        }

        private string logUserLogin(string username, string sessionID)
        {
            try
            {
                OperationContext context = OperationContext.Current;
                MessageProperties prop = context.IncomingMessageProperties;
                RemoteEndpointMessageProperty endPoint = prop[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                string ip = endPoint.Address;

                return SQLq.logUserLogin(username, ip, sessionID);
            }
            catch { return "error: exception (logUserLogin)"; }
        }

        public string deleteHDDItem(string sessionID, int hddSlot)
        {
            try
            {
                //find user
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return "error: invalid session data";

                return SQLq.deleteHDDslot(userID.ToString(), hddSlot.ToString());
            }
            catch { return "error: exception (deleteHDDItem)"; }
        }

        public string deleteCPUItem(string sessionID, int cpuSlot)
        {
            try
            {
                //find user
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return "error: invalid session data";

                return SQLq.deleteCPUslot(userID.ToString(), cpuSlot.ToString());
            }
            catch { return "error: exception (deleteCPUItem)"; }
        }

        public string deleteMission(string sessionID)
        {
            try
            {
                //find user
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return "error: invalid session data";

                return SQLq.deleteMission(userID.ToString());
            }
            catch { return "error: exception (deleteMission)"; }
        }

        public string deleteSlave(string sessionID, int slaveID)
        {
            try
            {
                //find user
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return "error: invalid session data";

                return SQLq.deleteSlave(userID.ToString(), slaveID);
            }
            catch { return "error: exception (deleteSlave)"; }
        }

        public string checkClientVersion(string version)
        {
            try
            {
                if (!versionOK(version, SQLq.getMinClientVersion("primary")))
                    return "error: client version too low";

                return "client up to date";
            }
            catch { return "error: exception (checkClientVersion) [try again]"; }
        }

        private bool versionOK(string clientVer, string requiredVer)
        {
            try
            {
                List<string> client = new List<string>();
                client.AddRange(clientVer.Split('.').ToArray());
                List<string> needed = new List<string>();
                needed.AddRange(requiredVer.Split('.').ToArray());

                int end = Math.Min(client.Count, needed.Count);
                for (int i = 0; i < end; i++)
                {
                    if (Convert.ToInt32(client[i]) < Convert.ToInt32(needed[i]))
                        return false;
                    else if (Convert.ToInt32(client[i]) > Convert.ToInt32(needed[i]))
                        return true;
                }

                return true;
            }
            catch { return false; }
        }

        public string loadDeafaultHDD(string sessionID)
        {
            try
            {
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return "error: invalid session data";

                return SQLq.loadDefaultHDD(userID.ToString());
            }
            catch { return "error: server threw exception (loadDefaultHDD)"; }
        }

        public HardDrive getHDDInfo(string sessionID)
        {
            try
            {
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return null;

                this.userHDD = SQLq.getHDDstate(userID.ToString());

                if (userHDD == null || userHDD.Programs.Count == 0)
                {
                    loadDeafaultHDD(sessionID);
                    this.userHDD = SQLq.getHDDstate(userID.ToString());
                }

                if (userHDD != null)
                {
                    userHDD.DriveSize = SQLq.getHDDsize(userID.ToString());
                    if (userHDD.DriveSize == -1)
                    {
                        SQLq.setHDDsize(userID.ToString(), (6).ToString());
                        userHDD.DriveSize = SQLq.getHDDsize(userID.ToString());
                    }
                }

                return userHDD;
            }
            catch { return null; }
        }

        public CPUload getCPUInfo(string sessionID)
        {
            try
            {
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return null;

                this.userCPU = SQLq.getCPUstate(userID.ToString());

                //set CPU slots
                userCPU.TotalCPUslots = SQLq.getCPUnum(userID.ToString());
                if (userCPU.TotalCPUslots < 4)
                {
                    if (SQLq.setCPUnum(userID.ToString(), (4).ToString()))
                        this.userCPU = SQLq.getCPUstate(userID.ToString());
                    else
                        return null;
                    this.userCPU = SQLq.getCPUstate(userID.ToString());
                }

                return userCPU;
            }
            catch { return null; }
        }

        public ProgramTypes getProgramTypes(string sessionID)
        {
            try
            {
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return null;

                return SQLq.getProgramTypes();
            }
            catch { return null; }
        }

        public UserInfo getUserInfo(string sessionID)
        {
            try
            {
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return null;

                UserInfo tmp = SQLq.getUserInfo(userID.ToString());
                if (tmp.CpuSlots == -1)
                {
                    SQLq.setUserInfo(userID.ToString());
                    tmp = SQLq.getUserInfo(userID.ToString());
                }
                return tmp;
            }
            catch { return null; }
        }

        public CompilerJob getCompilerJob(string sessionID)
        {
            try
            {
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return null;

                return SQLq.getCompilerJob(userID.ToString());
            }
            catch { return null; }
        }

        public MissionList getMissionList(string sessionID)
        {
            try
            {
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return null;

                return SQLq.getMissionList();
            }
            catch { return null; }
        }

        public UserMission getUserMission(string sessionID)
        {
            try
            {
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return null;

                UserMission mission = SQLq.getUserMission(userID.ToString());
                if (mission.UserID == -1)
                    return null;
                return mission;
            }
            catch { return null; }
        }

        public SlaveListHW getSlaves(string sessionID)
        {
            try
            {
                //find user
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return null;

                //load slaves
                return SQLq.getSlaves(userID.ToString());
            }
            catch { return null; }
        }

        public UserStats getStats(string sessionID)
        {
            try
            {
                //find user
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return null;

                //load slaves
                return SQLq.getStats();
            }
            catch { return null; }
        }

        public string purchaseFromMarket(string sessionID, ProgramHW software)
        {
            try
            {
                //session check
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return "error: invalid session info";

                //HDD checks
                HardDrive usrHDD = getHDDInfo(sessionID);
                if (software.HddSlot >= usrHDD.DriveSize)
                    return "error: invalid slot";

                bool slotEmpty = true;
                foreach (ProgramHW pr in usrHDD.Programs)
                    if (pr.HddSlot == software.HddSlot)
                    {
                        slotEmpty = false;
                        break;
                    }
                if (!slotEmpty)
                    return "error: slot already taken";

                //program checks
                ProgramTypes progs = SQLq.getProgramTypes();
                ProgramHW current = null;
                foreach (ProgramHW pr in progs.ProgramTypesLst)
                    if (pr.ProgramType == software.ProgramType && pr.ProgramSubType == software.ProgramSubType)
                    {
                        current = pr;
                        break;
                    }

                if (current == null)
                    return "error: invalid fileType";

                current.HddSlot = software.HddSlot;
                current.ProgramVersion = 1;
                current.UsesLeft = 5;

                //money checks
                UserInfo usrInfo = SQLq.getUserInfo(userID.ToString());
                if (usrInfo.UserCash < current.BasePrice)
                    return "error: not enough money";
                
                //commit
                string tmp = SQLq.setHDDslot(userID.ToString(), current);
                if (tmp.IndexOf("error") != -1)
                    return "error saving to HDD";

                tmp = SQLq.updateUserMoney(userID.ToString(), (long)-current.BasePrice);
                if (tmp.IndexOf("error") != -1)
                    return "error changing cash balance";

                //update log
                SQLq.updateLogs(userID.ToString(), (int)SQLqueries.messageType.cashTransaction, "Purchased " + current.ProgramName + " v(" + software.ProgramVersion + ".0)",
                                (int)current.BasePrice);

                return "info: successful purchase";
            }
            catch { return "error: exception (purchaseFromMarket)"; }
        }

        public string addCPUprogram(string sessionID, ProgramHW program, int slot)
        {
            try
            {
                //session check
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return "error: invalid session info";

                //CPU checks
                CPUload usrCPU = getCPUInfo(sessionID);
                if (slot >= userCPU.TotalCPUslots)
                    return "error: invalid slot";

                //set slot to program
                string tmp = SQLq.setCPUslot(userID.ToString(), program, slot.ToString());
                if (tmp.IndexOf("error") != -1)
                    return tmp;

                //decrement program usage count
                tmp = SQLq.updateHDDslotUses(userID.ToString(), program);
                if (tmp.IndexOf("error") != -1)
                    return tmp;

                //log start of program
                tmp = SQLq.updateLogs(userID.ToString(), (int)SQLqueries.messageType.programStart, "Started (local) " + program.ProgramName + " v(" + program.ProgramVersion + ".0)", 0);
                if (tmp.IndexOf("error") != -1)
                    return tmp;

                return "info: program started";
            }
            catch { return "error: exception (addCPUprogram)"; }
        }

        public string addCompilerJob(string sessionID, CompilerJob job)
        {
            try
            {
                //check user session
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return "error: invalid session data";

                //program checks
                ProgramTypes progs = SQLq.getProgramTypes();
                ProgramHW current = null;
                foreach (ProgramHW pr in progs.ProgramTypesLst)
                    if (pr.ProgramType == job.ProgramType && pr.ProgramSubType == job.ProgramSubType)
                    {
                        current = pr;
                        break;
                    }

                if (current == null)
                    return "error: invalid program specification";

                //check userMoney
                int bill = 0;
                if (job.ProgramVersion == 3)
                    bill = 500;
                else if (job.ProgramVersion > 3)
                    bill = 1500;
                int goldBill = job.ProgramVersion == 5 ? 1 : 0;

                UserInfo usrInfo = getUserInfo(sessionID);
                if (usrInfo.UserCash < bill)
                    return "error: not enough money for job";
                else if (usrInfo.UserGold < goldBill)
                    return "error: not eough gold for job";

                //assign buddy
                //todo, once social is implemented
                if (job.ProgramVersion >= 4)
                    job.BuddyName = "anon";
                else
                    job.BuddyName = "none";

                //assign job
                if (!SQLq.setCompilerJob(userID.ToString(), job))
                    return "error assigning job";

                //log job
                string message = "Started programming " + current.ProgramName + " v(" + job.ProgramVersion.ToString() + ".0)";
                SQLq.updateLogs(userID.ToString(), (int)SQLqueries.messageType.compilerStart, message, bill);

                //bill for the job
                SQLq.updateUserMoney(userID.ToString(), (long)-bill);
                if (goldBill != 0)
                    SQLq.updateUserGold(userID.ToString(), (long)-goldBill);

                //log bill
                message = "Programming expenses for " + current.ProgramName + " v(" + job.ProgramVersion.ToString() + ".0) $" + bill.ToString() + " [" + goldBill + "]";
                SQLq.updateLogs(userID.ToString(), (int)SQLqueries.messageType.cashTransaction, message, bill);

                return "info: started programming job";
            }
            catch { return null; }
        }

        public string addHDDslot(string sessionID)
        {
            try
            {
                //check user session
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return "error: invalid session data";

                //get hddinfo
                HardDrive userHDD = getHDDInfo(sessionID);
                if (userHDD == null)
                    return "error: no hdd info";

                //check number of slots
                if (userHDD.DriveSize >= 20)
                    return "error: hdd at max";

                //calculate cost
                int hddSlots = userHDD.DriveSize;
                int hddCost = (hddSlots / 4 - 1) * 1000 + (hddSlots % 4) * 250;
                int hddGold = hddSlots >= 16 ? 1 : 0;

                //check cash
                UserInfo usrInfo = getUserInfo(sessionID);
                if (usrInfo == null)
                    return "error: no user info";

                if (usrInfo.UserCash < hddCost)
                    return "error: insufficient funds";
                else if (usrInfo.UserGold < hddGold)
                    return "error: insufficient gold";

                //add slot
                if (!SQLq.addHDDSlot(userID.ToString()))
                    return "error: couldn't add hdd slot";

                //bill for the job
                SQLq.updateUserMoney(userID.ToString(), (long)-hddCost);
                if (hddGold != 0)
                    SQLq.updateUserGold(userID.ToString(), (long)-hddGold);

                //log bill
                string message = "Hardware purchase of HDD slot for $" + hddCost.ToString("N0") + " [" + hddGold.ToString() + "]";
                SQLq.updateLogs(userID.ToString(), (int)SQLqueries.messageType.cashTransaction, message, hddCost);
            }
            catch { return "error: exception at (addHDDslot)"; }

            return "info: new hdd slot acquired";
        }

        public string addCPUslot(string sessionID)
        {
            try
            {
                //check user session
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return "error: invalid session data";

                //get hddinfo
                CPUload userCPU = getCPUInfo(sessionID);
                if (userCPU == null)
                    return "error: no hdd info";

                //check number of slots
                if (userCPU.TotalCPUslots >= 12)
                    return "error: cpu at max";

                //calculate cost
                int cpuSlots = userCPU.TotalCPUslots;
                int cpuCost = (cpuSlots / 4 - 1) * 1000 + (cpuSlots % 4 + 1) * 250;
                int cpuGold = cpuSlots >= 8 ? 1 : 0;

                //check cash
                UserInfo usrInfo = getUserInfo(sessionID);
                if (usrInfo == null)
                    return "error: no user info";

                if (usrInfo.UserCash < cpuCost)
                    return "error: insufficient funds";
                else if (usrInfo.UserGold < cpuGold)
                    return "error: insufficient gold";

                //add slot
                if (!SQLq.addCPUslot(userID.ToString()))
                    return "error: couldn't add hdd slot";

                //bill for the job
                SQLq.updateUserMoney(userID.ToString(), (long)-cpuCost);
                if (cpuGold != 0)
                    SQLq.updateUserGold(userID.ToString(), (long)-cpuGold);

                //log bill
                string message = "Hardware purchase of CPU slot for $" + cpuCost.ToString("N0") + " [" + cpuGold.ToString() + "]";
                SQLq.updateLogs(userID.ToString(), (int)SQLqueries.messageType.cashTransaction, message, cpuCost);
            }
            catch { return "error: exception at (addCPUslot)"; }

            return "info: new hdd slot acquired";
        }

        public string addMission(string sessionID, UserMission mission)
        {
            try
            {
                //check user session
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return "error: invalid session data";

                mission.UserID = userID;

                //get and check current mission
                UserMission current = SQLq.getUserMission(userID.ToString());
                if (current.UserID != -1)
                    return "error: mission already active";

                //validate mission
                if (!validateMission(mission, userID))
                    return "error: incorrect mission info";

                //set mission
                if (!SQLq.setUserMission(mission))
                    return "error: couldn't set user mission";

                //generate mission slave
                Slave missionSlave = generateMissionSlave(mission, userID);
                if (missionSlave == null)
                    return "error: exception (generateMissionSlave)";

                //write slave and slots
                if (!SQLq.setSlave(userID.ToString(), missionSlave))
                    return "error: couldn't write slave";

                //remember to check for missing mission slave on mission completion


                return "info: set user mission";
            }
            catch { return "error: exception at (addMission)"; }
        }

        public string swapProgsHDD(string sessionID, int slotA, int slotB)
        {
            try
            {
                //session check
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return "error: invalid session info";

                //do swap
                return SQLq.swapHDDslots(userID.ToString(), slotA.ToString(), slotB.ToString());
            }
            catch { return "error: exception (swapProgsHDD)"; }
        }

        public string claimCompilerJob(string sessionID)
        {
            try
            {
                //session check
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return "error: invalid session info";

                //get job
                CompilerJob job = SQLq.getCompilerJob(userID.ToString());

                //check time
                if(job.EndTime > DateTime.UtcNow.Ticks) 
                    return "error: job in progress";

                //get hdd info
                HardDrive hdd = SQLq.getHDDstate(userID.ToString());

                //look for empty slot
                int freeSlot = -1;
                for (int i = 0; i < hdd.DriveSize; i++)
                {
                    bool free = true;
                    for (int c = 0; c < hdd.Programs.Count; c++)
                        if (hdd.Programs[c].HddSlot == i)
                        {
                            free = false;
                            break;
                        }

                    if (free)
                    {
                        freeSlot = i;
                        break;
                    }
                }

                if (freeSlot == -1)
                    return "error: no free HDD slots";

                //make program
                ProgramHW prog = new ProgramHW();
                prog.HddSlot = freeSlot;
                prog.ProgramType = job.ProgramType;
                prog.ProgramSubType = job.ProgramSubType;
                prog.ProgramVersion = job.ProgramVersion;
                prog.UsesLeft = 5;

                SQLq.setHDDslot(userID.ToString(), prog);

                //delete job
                SQLq.deleteCompilerJob(userID.ToString());
            }
            catch { return "error: exception (claimCompilerJob)"; }

            return "info: programming complete";
        }

        public string claimMission(string sessionID)
        {
            try
            {
                //find user
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return "error: invalid session data";

                //get mission
                UserMission mission = getUserMission(sessionID);
                if (mission == null)
                {
                    //might not need this
                    deleteMission(sessionID);
                    return "error: no active mission";
                }

                //get slaves
                SlaveListHW slaveList = getSlaves(sessionID);

                //check if mission is finnished
                if (!validateMissionEnd(mission, slaveList))
                    return "error: mission not complete";

                //claculate income and compensate player
                MissionList missionList = getMissionList(sessionID);
                if (missionList == null)
                    return "error: couldn't get mission list";

                int income = getMissionTypeByID(missionList, mission.MissionID).MissionPay;
                income += mission.PassStrength * 75;
                income += mission.ProgramVersion * 100;
                income += mission.NeedAdmin ? 0 : 250;

                SQLq.updateUserMoney(userID.ToString(), income);

                //log
                SQLq.updateLogs(userID.ToString(), (int)SQLqueries.messageType.cashTransaction, "Payment for mission $" + income.ToString("N0"), income);

                //give user hat-points
                SQLq.setUserHatPoints(userID.ToString(), getMissionTypeByID(missionList, mission.MissionID).HatPoints);

                //check if mission is 0, add slave
                if (mission.MissionID == 0)
                {
                    
                    int sID = getFreeSlave(slaveList);
                    if (sID == -1)
                    {
                        //remove mission
                        if (deleteMission(sessionID).IndexOf("error") != -1)
                            return "error: couldn't remove mission";

                        return "info: well done, but there are no free slave slots";
                    }

                    Slave missionSlave = getSlaveByID(slaveList, 999);
                    if (missionSlave != null)
                    {
                        missionSlave.EndTime = generateEndTime(missionSlave);
                        missionSlave.SlaveID = sID;
                        SQLq.setSlave(userID.ToString(), missionSlave);
                    }
                }

                //remove mission
                if (deleteMission(sessionID).IndexOf("error") != -1)
                    return "error: couldn't remove mission";

                return "info: job well done";
            }
            catch { return "error: exception (deleteMission)"; }
        }
        
        public string claimDonation(string sessionID)
        {
            try
            {
                //find user
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return "error: invalid session data";

                //set slave
                if (!SQLq.setGold(userID.ToString(), 150))
                    return "error: couldn't set gold";

                return "info: gold updated, thank you";
            }
            catch { return "error: exception (claimDonation)"; }
        }

        public string setSlave(string sessionID, Slave slave)
        {
            try
            {
                //find user
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return "error: invalid session data";

                //set slave
                if (!SQLq.setSlave(userID.ToString(), slave))
                    return "error: couldn't set slave";

                return "info: slave updated";
            }
            catch { return "error: exception (setSlave)"; }
        }

        public string decrementHDDuses(string sessionID, ProgramHW program)
        {
            try
            {
                //session check
                long userID = SQLq.getUserIDFromSession(sessionID);
                if (userID == -1)
                    return "error: invalid session info";

                //decrement program usage count
                return SQLq.updateHDDslotUses(userID.ToString(), program);
            }
            catch { return "error: exception (decrementHDDuses)"; }
        }

        private bool validateMission(UserMission mission, long userID)
        {
            if (mission.UserID != userID)
                return false;

            switch (mission.MissionID)
            {
                //Hack a random target and slave it.
                case 0:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (mission.NeedAdmin)
                        return false;
                    if (mission.HavePass)
                        return false;
                    if (mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 4)
                        return false;
                    if (mission.ProgramSubGroup != 7)
                        return false;
                    if (mission.ProgramVersion != mission.PassStrength)
                        return false;
                    break;
                //Install %1 %2 on client computer.
                case 1:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (mission.NeedAdmin)
                        return false;
                    if (mission.HavePass)
                        return false;
                    if (!mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 1)
                        return false;
                    if (mission.ProgramSubGroup < 1 || mission.ProgramSubGroup > 7)
                        return false;
                    if (mission.ProgramVersion < 1 || mission.ProgramVersion > 4)
                        return false;
                    break;
                //Break in and install %1 %2 on victim computer.
                case 2:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (mission.NeedAdmin)
                        return false;
                    if (mission.HavePass)
                        return false;
                    if (mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 1)
                        return false;
                    if (mission.ProgramSubGroup < 1 || mission.ProgramSubGroup > 7)
                        return false;
                    if (mission.ProgramVersion < 1 || mission.ProgramVersion > 4)
                        return false;
                    break;
                //Upgrade client's %1 to %2.
                case 3:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (mission.NeedAdmin)
                        return false;
                    if (!mission.HavePass)
                        return false;
                    if (!mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 2)
                        return false;
                    if (mission.ProgramSubGroup < 1 || mission.ProgramSubGroup > 5)
                        return false;
                    if (mission.ProgramVersion < 2 || mission.ProgramVersion > 4)
                        return false;
                    break;
                //Break in and downgrade target's %1 to %2
                case 4:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (mission.NeedAdmin)
                        return false;
                    if (mission.HavePass)
                        return false;
                    if (mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 2)
                        return false;
                    if (mission.ProgramSubGroup < 1 || mission.ProgramSubGroup > 5)
                        return false;
                    if (mission.ProgramVersion != 1)
                        return false;
                    break;
                //Remove unwanted %1 from client computer.
                case 5:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (!mission.NeedAdmin)
                        return false;
                    if (!mission.HavePass)
                        return false;
                    if (!mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 4)
                        return false;
                    if (mission.ProgramSubGroup < 1 || mission.ProgramSubGroup > 6)
                        return false;
                    if (mission.ProgramVersion < 1 || mission.ProgramVersion > 4)
                        return false;
                    break;
                //Break in and format target HDD.
                case 6:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (!mission.NeedAdmin)
                        return false;
                    if (mission.HavePass)
                        return false;
                    if (mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 1)
                        return false;
                    if (mission.ProgramSubGroup != 3)
                        return false;
                    if (mission.ProgramVersion != -1)
                        return false;
                    break;
                //Client forgot his password, crack it for him.
                case 7:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (mission.NeedAdmin)
                        return false;
                    if (mission.HavePass)
                        return false;
                    if (mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 4)
                        return false;
                    if (mission.ProgramSubGroup < 1 || mission.ProgramSubGroup > 8)
                        return false;
                    if (mission.ProgramVersion != mission.PassStrength)
                        return false;
                    break;
                //Crack victim password.
                case 8:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (mission.NeedAdmin)
                        return false;
                    if (mission.HavePass)
                        return false;
                    if (mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 4)
                        return false;
                    if (mission.ProgramSubGroup < 1 || mission.ProgramSubGroup > 8)
                        return false;
                    if (mission.ProgramVersion != mission.PassStrength)
                        return false;
                    break;
                //Install a backdoor for client.
                case 9:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (mission.NeedAdmin)
                        return false;
                    if (mission.HavePass)
                        return false;
                    if (mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 4)
                        return false;
                    if (mission.ProgramSubGroup != 4)
                        return false;
                    if (mission.ProgramVersion != mission.PassStrength)
                        return false;
                    break;
                //Alter victim database.
                case 10:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (mission.NeedAdmin)
                        return false;
                    if (mission.HavePass)
                        return false;
                    if (mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 2)
                        return false;
                    if (mission.ProgramSubGroup != 4)
                        return false;
                    if (mission.ProgramVersion != mission.PassStrength)
                        return false;
                    break;
                //Develop and deliver %1 %2 for client.
                case 11:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (mission.NeedAdmin)
                        return false;
                    if (!mission.HavePass)
                        return false;
                    if (!mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 4)
                        return false;
                    if (mission.ProgramSubGroup < 1 || mission.ProgramSubGroup > 9)
                        return false;
                    if (mission.ProgramVersion != 5)
                        return false;
                    break;
                //Develop and deliver %1 %2 for client.
                case 12:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (mission.NeedAdmin)
                        return false;
                    if (!mission.HavePass)
                        return false;
                    if (!mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 3)
                        return false;
                    if (mission.ProgramSubGroup < 1 || mission.ProgramSubGroup > 3)
                        return false;
                    if (mission.ProgramVersion != 5)
                        return false;
                    break;
                //Gain admin privlages on victim computer.
                case 13:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (!mission.NeedAdmin)
                        return false;
                    if (mission.HavePass)
                        return false;
                    if (mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 4)
                        return false;
                    if (mission.ProgramSubGroup < 7 || mission.ProgramSubGroup > 8)
                        return false;
                    if (mission.ProgramVersion != mission.PassStrength)
                        return false;
                    break;
                //Setup a scam website.
                case 14:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (!mission.NeedAdmin)
                        return false;
                    if (mission.HavePass)
                        return false;
                    if (mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 2)
                        return false;
                    if (mission.ProgramSubGroup != 2)
                        return false;
                    if (mission.ProgramVersion != mission.PassStrength)
                        return false;
                    break;
                //Setup IRC bot-net controller.
                case 15:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (!mission.NeedAdmin)
                        return false;
                    if (mission.HavePass)
                        return false;
                    if (mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 2)
                        return false;
                    if (mission.ProgramSubGroup != 3)
                        return false;
                    if (mission.ProgramVersion != mission.PassStrength)
                        return false;
                    break;
                //Setup SQL database for client.
                case 16:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (!mission.NeedAdmin)
                        return false;
                    if (!mission.HavePass)
                        return false;
                    if (!mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 2)
                        return false;
                    if (mission.ProgramSubGroup != 4)
                        return false;
                    if (mission.ProgramVersion != mission.PassStrength)
                        return false;
                    break;
                //Setup a web-server for client.
                case 17:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (!mission.NeedAdmin)
                        return false;
                    if (!mission.HavePass)
                        return false;
                    if (!mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 2)
                        return false;
                    if (mission.ProgramSubGroup != 2)
                        return false;
                    if (mission.ProgramVersion != mission.PassStrength)
                        return false;
                    break;
                //Install a keylogger on victim PC.
                case 18:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (!mission.NeedAdmin)
                        return false;
                    if (mission.HavePass)
                        return false;
                    if (mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 4)
                        return false;
                    if (mission.ProgramSubGroup != 6)
                        return false;
                    if (mission.ProgramVersion != mission.PassStrength)
                        return false;
                    break;
                //Install root-kit on victim computer.
                case 19:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (!mission.NeedAdmin)
                        return false;
                    if (mission.HavePass)
                        return false;
                    if (mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 4)
                        return false;
                    if (mission.ProgramSubGroup != 5)
                        return false;
                    if (mission.ProgramVersion != mission.PassStrength)
                        return false;
                    break;
                //Map web-server.
                case 20:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (mission.NeedAdmin)
                        return false;
                    if (mission.HavePass)
                        return false;
                    if (mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 2)
                        return false;
                    if (mission.ProgramSubGroup != 2)
                        return false;
                    if (mission.ProgramVersion != mission.PassStrength)
                        return false;
                    break;
                //Map ftp-server.
                case 21:
                    if (mission.PassStrength < 1 || mission.PassStrength > 4)
                        return false;
                    if (mission.NeedAdmin)
                        return false;
                    if (mission.HavePass)
                        return false;
                    if (mission.HaveAdmin)
                        return false;
                    if (mission.ProgramGroup != 2)
                        return false;
                    if (mission.ProgramSubGroup != 1)
                        return false;
                    if (mission.ProgramVersion != mission.PassStrength)
                        return false;
                    break;
                default:
                    return false;
                    //break;
            }

            return true;
        }

        private bool validateMissionEnd(UserMission mission,SlaveListHW slaves)
        {
            Slave missionSlave = getSlaveByID(slaves, 999);

            //check mission requirements
            switch (mission.MissionID)
            {
                case 0:
                    if (missionSlave.UserPass || missionSlave.AdminPass)
                        return true;
                    break;
                case 1:
                case 2:
                case 3:
                case 4:
                    if (slaveHasProgram(slaves, 999, mission.ProgramGroup, mission.ProgramSubGroup, mission.ProgramVersion, mission.ProgramVersion))
                        return true;
                    break;
                case 9:
                case 11:
                case 12:
                case 14:
                case 15:
                case 16:
                case 17:
                case 18:
                case 19:
                    if (slaveHasProgram(slaves, 999, mission.ProgramGroup, mission.ProgramSubGroup, 0, 5))
                        return true;
                    break;
                case 5:
                    if (!slaveHasProgram(slaves, 999, mission.ProgramGroup, mission.ProgramSubGroup, mission.ProgramVersion, mission.ProgramVersion))
                        return true;
                    break;
                case 6:
                    if (missionSlave.SlaveFiles.Count == 0)
                        return true;
                    break;
                case 10:
                case 20:
                case 21:
                    return true;
                case 7:
                case 8:
                    if (missionSlave.UserPass)
                        return true;
                    break;
                case 13:
                    if (missionSlave.AdminPass)
                        return true;
                    break;

                default:
                    return false;
            }

            return false;
        }

        private bool slaveHasProgram(SlaveListHW slaves, int slaveID, int progGroup, int progSub, int minProgVer, int maxProgVer)
        {
            Slave slave = getSlaveByID(slaves, slaveID);
            if (slave == null)
                return false;

            foreach (SlaveSlot slot in slave.SlaveFiles)
                if (slot.ProgramGroup == progGroup && slot.ProgramSubGroup == progSub && slot.ProgramVersion >= minProgVer && slot.ProgramVersion <= maxProgVer)
                    return true;

            return false;
        }

        private int getFreeSlave(SlaveListHW slaves)
        {
            for (int c = 0; c < 4; c++)
            {
                bool free = true;
                foreach(Slave sl in slaves.SlaveList)
                    if (sl.SlaveID == c)
                    {
                        free = false;
                        break;
                    }

                if (free)
                    return c;
            }
            return -1;
        }

        private Slave getSlaveByID(SlaveListHW slaves, int id)
        {
            foreach (Slave sl in slaves.SlaveList)
                if (sl.SlaveID == id)
                    return sl;
            return null;
        }

        private MissionType getMissionTypeByID(MissionList list, int id)
        {
            foreach (MissionType mt in list.Missions)
                if (mt.MissionID == id)
                    return mt;
            return null;
        }

        private Slave generateMissionSlave(UserMission mission, long userID)
        {
            try
            {
                Random rnd = new Random((int)DateTime.UtcNow.Ticks);

                Slave missionSlave = new Slave();
                missionSlave.UserID = userID;
                missionSlave.SlaveID = 999;
                missionSlave.EndTime = -1;
                missionSlave.UserPass = mission.HavePass;
                missionSlave.AdminPass = mission.HaveAdmin;

                //set files
                for (int i = 0; i < 6; i++)
                {
                    SlaveSlot slot = new SlaveSlot();
                    slot.UserID = userID;
                    slot.SlaveID = missionSlave.SlaveID;
                    slot.SlotID = i;

                    int group = rnd.Next(4) + 1;
                    int sub = -1;
                    switch (group)
                    {
                        case 1:
                            sub = rnd.Next(7) + 1;
                            break;
                        case 2:
                            sub = rnd.Next(5) + 1;
                            break;
                        case 3:
                            sub = rnd.Next(3) + 1;
                            break;
                        case 4:
                            sub = rnd.Next(9) + 1;
                            break;
                        default:
                            break;
                    }

                    slot.ProgramGroup = group;
                    slot.ProgramSubGroup = sub;
                    slot.ProgramVersion = rnd.Next(4) + 1;

                    if (i >= 3 && slot.ProgramVersion < 4)
                        slot.ProgramVersion++;

                    missionSlave.SlaveFiles.Add(slot);
                }

                //add mission specific programs
                if (mission.MissionID == 1 || mission.MissionID == 11 || mission.MissionID == 12)//needs empty slot
                {
                    if (mission.PassStrength < 3 && !mission.NeedAdmin)
                        missionSlave.SlaveFiles.RemoveAt(0);
                    else
                        missionSlave.SlaveFiles.RemoveAt(3);
                }
                else if (mission.MissionID == 3)//upgrade
                {
                    SlaveSlot slot = new SlaveSlot();
                    slot.UserID = userID;
                    slot.SlaveID = missionSlave.SlaveID;

                    if (mission.NeedAdmin)
                    {
                        slot.SlotID = rnd.Next(3) + 3;
                        missionSlave.SlaveFiles.RemoveAt(slot.SlotID);
                    }
                    else
                    {
                        slot.SlotID = rnd.Next(3);
                        missionSlave.SlaveFiles.RemoveAt(slot.SlotID);
                    }

                    slot.ProgramGroup = mission.ProgramGroup;
                    slot.ProgramSubGroup = mission.ProgramSubGroup;
                    slot.ProgramVersion = 1;
                    missionSlave.SlaveFiles.Insert(slot.SlotID, slot);
                }
                else if (mission.MissionID == 4)//downgrade
                {
                    SlaveSlot slot = new SlaveSlot();
                    slot.UserID = userID;
                    slot.SlaveID = missionSlave.SlaveID;

                    slot.SlotID = rnd.Next(3);
                    missionSlave.SlaveFiles.RemoveAt(slot.SlotID);

                    slot.ProgramGroup = mission.ProgramGroup;
                    slot.ProgramSubGroup = mission.ProgramSubGroup;
                    slot.ProgramVersion = rnd.Next(4) + 1;
                    missionSlave.SlaveFiles.Insert(slot.SlotID, slot);
                }
                else if (mission.MissionID == 5)//remove
                {
                    SlaveSlot slot = new SlaveSlot();
                    slot.UserID = userID;
                    slot.SlaveID = missionSlave.SlaveID;
                    slot.SlotID = rnd.Next(3) + 3;
                    missionSlave.SlaveFiles.RemoveAt(slot.SlotID);

                    slot.ProgramGroup = mission.ProgramGroup;
                    slot.ProgramSubGroup = mission.ProgramSubGroup;
                    slot.ProgramVersion = mission.ProgramVersion;
                    missionSlave.SlaveFiles.Insert(slot.SlotID, slot);
                }
                else if (mission.MissionID == 20 || mission.MissionID == 21)//needs web-server, ftp-server
                {
                    SlaveSlot slot = new SlaveSlot();
                    slot.UserID = userID;
                    slot.SlaveID = missionSlave.SlaveID;
                    slot.SlotID = rnd.Next(3);
                    missionSlave.SlaveFiles.RemoveAt(slot.SlotID);

                    slot.ProgramGroup = mission.ProgramGroup;
                    slot.ProgramSubGroup = mission.ProgramSubGroup;
                    slot.ProgramVersion = mission.ProgramVersion;
                    missionSlave.SlaveFiles.Insert(slot.SlotID, slot);
                }

                return missionSlave;
            }
            catch { return null; }
        }

        private long generateEndTime(Slave slave)
        {
            long endTime = TimeSpan.TicksPerMinute * 5;
            foreach (SlaveSlot slot in slave.SlaveFiles)
                if (slot.ProgramGroup == 4 && slot.ProgramSubGroup == 3)
                    endTime += TimeSpan.TicksPerMinute * 5 * slot.ProgramVersion;
                else if (slot.ProgramGroup == 4 && slot.ProgramSubGroup == 4)
                    endTime += TimeSpan.TicksPerHour * slot.ProgramVersion;
                else if (slot.ProgramGroup == 4 && slot.ProgramSubGroup == 5)
                    endTime += TimeSpan.TicksPerDay * slot.ProgramVersion;

            endTime += DateTime.UtcNow.Ticks;

            return endTime;
        }
    }

    public class SessionData
    {
        private string userName;
        private long userID;
        private string sessionID;
        private DateTime sessionStart;
        private Random rand = new Random((int)DateTime.Now.Ticks);

        public string UserName { get { return userName; } set { userName = value; } }
        public long UserID { get { return userID; } set { userID = value; } }
        public string SessionID { get { return sessionID; } set { sessionID = value; } }
        public DateTime SessionStart { get { return sessionStart; } set { sessionStart = value; } }
        public Random Rand { get { return rand; } set { rand = value; } }

        public string setSession(string username, string password, long userid)
        {
            userName = username;
            userID = userid;
            sessionStart = DateTime.Now.ToUniversalTime();
            sessionID = new Crypto().getSHA1hash(username + password + sessionStart.Ticks.ToString());

            return sessionID;
        }
    }
}
