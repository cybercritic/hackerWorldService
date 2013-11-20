using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;

namespace hackerWorldService
{
    public class SQLqueries
    {
        public enum messageType { cashTransaction, programStart, compilerStart }

        private System.Data.SqlClient.SqlConnectionStringBuilder conStr = null;
        
        public SqlConnection SQLraw = new SqlConnection();

        public SQLqueries()
        {
            try
            {
                this.setSQLconnection();
                SQLraw.ConnectionString = conStr.ConnectionString;
                SQLraw.Open();
            }
            catch { }
        }

        ~SQLqueries()
        {
            //if (SQLraw.State == System.Data.ConnectionState.Open)
            //    SQLraw.Close();
        }

        private void setSQLconnection()
        {
            conStr = new SqlConnectionStringBuilder();
            conStr.UserInstance = false;
            conStr.NetworkLibrary = "DBMSSOCN";
            conStr.DataSource = "localhost";
            conStr.InitialCatalog = "hackerWorldDB";
            conStr.IntegratedSecurity = false;
            conStr.UserID = "hackerWorldDBuser";
            conStr.Password = "Tx3NA573PeeR8.1";
        }

        /// <summary>
        /// add user
        /// </summary>
        /// <param name="username">username</param>
        /// <param name="password">password hash</param>
        /// <param name="email">email</param>
        /// <returns></returns>
        public string addUser(string username, string password, string email)
        {
            if (userExists(username))
                return "error: user exists";
            else if (emailExists(email))
                return "error: email already registered";
            else
                return insertUser(username, password, email);
        }

        /// <summary>
        /// logs user login
        /// </summary>
        /// <param name="username"></param>
        /// <param name="ip"></param>
        public string logUserLogin(string username, string ip, string sessionID)
        {
            string timeTicksUTC = DateTime.Now.ToUniversalTime().Ticks.ToString();

            string cmdStr = " INSERT INTO UserLoginLog (userID,loginTime,userIP,sessionID)" +
                            " SELECT u.userID , @logintime, @userip, @sessionID " +
                            " FROM UserTable u" +
                            " WHERE u.username = @username ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@userip", ip);
            command.Parameters.AddWithValue("@logintime", timeTicksUTC);
            command.Parameters.AddWithValue("@sessionID", sessionID);

            try { command.ExecuteNonQuery(); }
            catch 
            {
                cmdStr =    " UPDATE UserLoginLog " +
                            " SET loginTime = @logintime , userIP = @userip , sessionID = @sessionID" +
                            " WHERE userID = (SELECT u.userID "+
                            "                 FROM UserTable u " +
                            "                 WHERE u.username = @username);";

                command = new SqlCommand(cmdStr, SQLraw);
                command.Parameters.AddWithValue("@username", username);
                command.Parameters.AddWithValue("@userip", ip);
                command.Parameters.AddWithValue("@logintime", timeTicksUTC);
                command.Parameters.AddWithValue("@sessionID", sessionID);

                try { command.ExecuteNonQuery(); }
                catch
                { return "error: couldn't set session data"; }
            }

            return "session data updated";
        }

        /// <summary>
        /// removes all files from hdd
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public string formatHDD(string userID)
        {
            string cmdStr = " DELETE FROM UserHDD " +
                            " WHERE userID = @user ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@user", userID);
            
            try { command.ExecuteNonQuery(); }
            catch { }

            return "info: HDD wiped";
        }

        /// <summary>
        /// add new user
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        private string insertUser(string username, string password, string email)
        {
            string cmdStr = " INSERT INTO UserTable (userName,userEmail,userPasswordHash)" +
                            " VALUES (@username , @email , @password);";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@email", email);
            command.Parameters.AddWithValue("@password", password);

            try { command.ExecuteNonQuery(); }
            catch { return "error adding user"; }

            return "info: created user";
        }

        /// <summary>
        /// change user password
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="hashed"></param>
        /// <returns></returns>
        public bool updatePassword(string username, string password, bool hashed)
        {

            string passwordHash = password;
            if(!hashed)
                passwordHash = new Crypto().getSHA1hash(password);
            
            string cmdStr = " UPDATE UserTable " +
                            " SET userPasswordHash = @passwordHash " +
                            " WHERE userName = @username ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@username", username);
            command.Parameters.AddWithValue("@passwordHash", passwordHash);

            try { command.ExecuteNonQuery(); }
            catch { return false; }

            return true;
        }

        /// <summary>
        /// remove copy of program
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="program"></param>
        /// <returns></returns>
        public string updateHDDslotUses(string userID, ProgramHW program)
        {
            string cmdStr = " UPDATE UserHDD " +
                            " SET usesLeft = usesLeft - 1 " +
                            " WHERE userID = @userID AND hddSlot = @hddSlot;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);
            command.Parameters.AddWithValue("@hddSlot", program.HddSlot);

            try { command.ExecuteNonQuery(); }
            catch { return "error: couldn't update HDDslot"; }

            if (program.UsesLeft - 1 <= 0)
                this.deleteHDDslot(userID, program.HddSlot.ToString());

            return "info: software usage recorded";
        }

        /// <summary>
        /// logging system, add to log
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="messageType"></param>
        /// <param name="messageText"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public string updateLogs(string userID, int messageType, string messageText, int amount)
        {
            //decrement current messages
            string cmdStr = " UPDATE LogTable " +
                            " SET messageID = messageID - 1 " +
                            " WHERE userID = @userID AND messageType = @messageType;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);
            command.Parameters.AddWithValue("@messageType", messageType.ToString());
            
            try { command.ExecuteNonQuery(); }
            catch { return "error: couldn't update LogTable"; }

            //insert new message
            cmdStr = " INSERT INTO LogTable (userID,messageType,messageID,messageText,messageInt)" +
                     " VALUES (@userID , @messageType , @messageID, @messageText, @messageInt);";

            command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);
            command.Parameters.AddWithValue("@messageType", messageType);
            command.Parameters.AddWithValue("@messageID", 10);
            command.Parameters.AddWithValue("@messageText", messageText);
            command.Parameters.AddWithValue("@messageInt", amount);

            try { command.ExecuteNonQuery(); }
            catch { return "error: logTable insert"; }

            //delete older messages
            cmdStr = " DELETE FROM LogTable " +
                     " WHERE userID = @userID AND messageType = @messageType AND messageID < 0;";

            command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);
            command.Parameters.AddWithValue("@messageType", messageType);
            
            try { command.ExecuteNonQuery(); }
            catch { return "error: logTable delete"; }

            return "info: updated log";
        }

        /// <summary>
        /// swap two hdd slots
        /// </summary>
        /// <param name="username"></param>
        /// <param name="slotA"></param>
        /// <param name="slotB"></param>
        /// <returns></returns>
        public string swapHDDslots(string userID, string slotA, string slotB)
        {
            ProgramHW progA = getProgramAtSlot(userID, slotA);
            ProgramHW progB = getProgramAtSlot(userID, slotB);

            if (progA.ProgramType <= 0)
                return "error: origin slot is empty";

            //move to empty slot
            if (progB.ProgramType <= 0)
            {
                progA.HddSlot = Convert.ToInt32(slotB);

                if (setHDDslot(userID, progA).IndexOf("error") != -1)
                    return "error setting slot";
                deleteHDDslot(userID, slotA);
                return "moved program to new slot";
            }

            //swap slots
            //might need to change this to commit all at once
            int tmp = progA.HddSlot;
            progA.HddSlot = progB.HddSlot;
            progB.HddSlot = tmp;

            deleteHDDslot(userID, slotA);
            setHDDslot(userID, progB);

            deleteHDDslot(userID, slotB);
            setHDDslot(userID, progA);

            return "info: swapped programs";
        }

        /// <summary>
        /// check if user with given name exists
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public bool userExists(string username)
        {
            int result = 0;

            username = username.ToLower();

            string cmdStr = "SELECT COUNT(*) AS count FROM UserTable " +
                            "WHERE LOWER(userName) = @username ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@username", username);

            SqlDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result = Convert.ToInt32(reader["count"]);
                }
                reader.Close();
            }
            catch { }

            return (result == 0 ? false : true);
        }

        /// <summary>
        /// returns username given email
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public string getUsernameFromEmail(string email)
        {
            string result = "";

            email = email.ToLower();
            
            string cmdStr = " SELECT userName FROM UserTable " +
                            " WHERE LOWER(userEmail) = @email ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@email", email);

            SqlDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result = Convert.ToString(reader["userName"]);
                }
                reader.Close();
            }
            catch { return null; }

            return result;
        }

        /// <summary>
        /// return user id key
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public long getUserIDFromName(string username)
        {
            long result = -1;

            username = username.ToLower();

            string cmdStr = " SELECT userID FROM UserTable " +
                            " WHERE LOWER(username) = @username ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@username", username);

            SqlDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result = Convert.ToInt64(reader["userID"]);
                }
                reader.Close();
            }
            catch { }

            return result;
        }

        /// <summary>
        /// return user id key
        /// </summary>
        /// <param name="sessionID"></param>
        /// <returns></returns>
        public long getUserIDFromSession(string sessionID)
        {
            long result = -1;

            sessionID = sessionID.ToLower();

            string cmdStr = " SELECT userID FROM UserLoginLog " +
                            " WHERE LOWER(sessionID) = @sessionID ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@sessionID", sessionID);

            SqlDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result = Convert.ToInt64(reader["userID"]);
                }
                reader.Close();
            }
            catch { }

            return result;
        }

        /// <summary>
        /// get program at hddSlot
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public ProgramHW getProgramAtSlot(string userID, string slotID)
        {
            ProgramHW result = new ProgramHW();

            string cmdStr = " SELECT * FROM UserHDD " +
                            " WHERE userID = @userID AND hddSlot = @slotID ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);
            command.Parameters.AddWithValue("@slotID", slotID);

            SqlDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result.HddSlot = Convert.ToInt32(reader["hddSlot"]);
                    result.ProgramType = Convert.ToInt32(reader["programType"]);
                    result.ProgramSubType = Convert.ToInt32(reader["programSubType"]);
                    result.UsesLeft = Convert.ToInt32(reader["usesLeft"]);
                    result.ProgramVersion = Convert.ToInt32(reader["programVersion"]);
                }
                reader.Close();
            }
            catch { return null; }

            return result;
        }

        /// <summary>
        /// get user hdd state
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public HardDrive getHDDstate(string userID)
        {
            HardDrive result = new HardDrive();

            string cmdStr = " SELECT * FROM UserHDD " +
                            " WHERE userID = @userID ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);

            SqlDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    ProgramHW program = new ProgramHW();

                    program.HddSlot = Convert.ToInt32(reader["hddSlot"]);
                    program.ProgramType = Convert.ToInt32(reader["programType"]);
                    program.ProgramSubType = Convert.ToInt32(reader["programSubType"]);
                    program.UsesLeft = Convert.ToInt32(reader["usesLeft"]);
                    program.ProgramVersion = Convert.ToInt32(reader["programVersion"]);
                    result.Programs.Add(program);
                }
                reader.Close();
            }
            catch { return null; }

            result.DriveSize = getHDDsize(userID);

            return result;
        }

        /// <summary>
        /// get user hdd state
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public CPUload getCPUstate(string userID)
        {
            CPUload result = new CPUload();

            string cmdStr = " SELECT * FROM UserCPU " +
                            " WHERE userID = @userID ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);

            SqlDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    CPUslotHW program = new CPUslotHW();

                    program.CpuSlot = Convert.ToInt32(reader["cpuSlot"]);
                    program.ProgramType = Convert.ToInt32(reader["programType"]);
                    program.ProgramSubType = Convert.ToInt32(reader["programSubType"]);
                    program.ProgramVersion = Convert.ToInt32(reader["programVersion"]);
                    result.Programs.Add(program);
                }
                reader.Close();
            }
            catch { return null; }

            return result;
        }
        
        /// <summary>
        /// returns all known filetypes
        /// </summary>
        /// <returns></returns>
        public ProgramTypes getProgramTypes()
        {
            ProgramTypes result = new ProgramTypes();

            string cmdStr = " SELECT * FROM ProgramTypes; ";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            
            SqlDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    ProgramHW program = new ProgramHW();

                    program.ProgramType = Convert.ToInt32(reader["programType"]);
                    program.ProgramSubType = Convert.ToInt32(reader["programSubType"]);
                    program.ProgramName = Convert.ToString(reader["programName"]);
                    program.ProgramDescription = Convert.ToString(reader["programDescription"]);
                    program.BasePrice = (float)Convert.ToDouble(reader["basePrice"]);
                    result.ProgramTypesLst.Add(program);
                }
                reader.Close();
            }
            catch { return null; }

            return result;
        }

        /// <summary>
        /// returns all known filetypes
        /// </summary>
        /// <returns></returns>
        public UserInfo getUserInfo(string userID)
        {
            UserInfo result = new UserInfo();
            result.CpuSlots = -1;

            string cmdStr = " SELECT * FROM UserInfo " +
                            " WHERE userID = @userID ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);

            SqlDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result.CpuSlots = Convert.ToInt32(reader["userCPUslots"]);
                    result.HddSlots = Convert.ToInt32(reader["userHDDslots"]);
                    result.UserCash = Convert.ToInt64(reader["userCash"]);
                    result.UserGold = Convert.ToInt64(reader["userGold"]);
                    result.UserHatPoints = Convert.ToInt32(reader["userHatPoints"]);
                }
                reader.Close();
            }
            catch { return null; }

            return result;
        }

        /// <summary>
        /// returns compiler job
        /// </summary>
        /// <returns></returns>
        public CompilerJob getCompilerJob(string userID)
        {
            CompilerJob result = new CompilerJob();
            result.Active = true;

            string cmdStr = " SELECT * FROM CompilerTable " +
                            " WHERE userID = @userID ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);

            SqlDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result.UserID = Convert.ToInt64(reader["userID"]);
                    result.ProgramType = Convert.ToInt32(reader["programType"]);
                    result.ProgramSubType = Convert.ToInt32(reader["programSubType"]);
                    result.ProgramVersion = Convert.ToInt32(reader["programVersion"]);
                    result.StartTime = Convert.ToInt64(reader["startTime"]);
                    result.EndTime = Convert.ToInt64(reader["endTime"]);
                    result.BuddyID = Convert.ToInt64(reader["buddyID"]);
                }
                reader.Close();
            }
            catch { return null; }

            if (result.UserID == -1)
                return null;
            return result;
        }

        /// <summary>
        /// returns missionTypes list
        /// </summary>
        /// <returns></returns>
        public MissionList getMissionList()
        {
            MissionList result = new MissionList();

            string cmdStr = " SELECT * FROM MissionTypes; ";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);

            SqlDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    MissionType mission = new MissionType();

                    mission.MissionID = Convert.ToInt32(reader["missionID"]);
                    mission.HatPoints = Convert.ToInt32(reader["hatPoints"]);
                    mission.MissionPay = Convert.ToInt32(reader["missionPay"]);
                    mission.Description = Convert.ToString(reader["missionDescription"]);
                    result.Missions.Add(mission);
                }
                reader.Close();
            }
            catch { return null; }

            return result;
        }

        /// <summary>
        /// returns current user mission
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public UserMission getUserMission(string userID)
        {
            UserMission result = new UserMission();
            result.UserID = -1;

            string cmdStr = " SELECT * FROM UserMission " +
                            " WHERE userID = @userID ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);

            SqlDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result.UserID = Convert.ToInt64(reader["userID"]);
                    result.MissionID = Convert.ToInt32(reader["missionID"]);
                    result.PassStrength = Convert.ToInt32(reader["passStrength"]);
                    result.NeedAdmin = Convert.ToBoolean(reader["needAdmin"]);
                    result.HavePass = Convert.ToBoolean(reader["havePass"]);
                    result.HaveAdmin = Convert.ToBoolean(reader["haveAdmin"]);
                    result.ProgramGroup = Convert.ToInt32(reader["programGroup"]);
                    result.ProgramSubGroup = Convert.ToInt32(reader["programSubGroup"]);
                    result.ProgramVersion = Convert.ToInt32(reader["programVersion"]);
                }
                reader.Close();
            }
            catch { return null; }

            return result;
        }

        /// <summary>
        /// returns user slaves and files
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public SlaveListHW getSlaves(string userID)
        {
            SlaveListHW result = new SlaveListHW();

            string cmdStr = " SELECT * FROM SlaveTable " +
                            " WHERE userID = @userID ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);

            //get user slaves
            SqlDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Slave slave = new Slave();

                    slave.UserID = Convert.ToInt64(reader["userID"]);
                    slave.SlaveID = Convert.ToInt32(reader["slaveID"]);
                    slave.EndTime = Convert.ToInt64(reader["endTime"]);
                    slave.UserPass = Convert.ToBoolean(reader["userPass"]);
                    slave.AdminPass = Convert.ToBoolean(reader["adminPass"]);
                    result.SlaveList.Add(slave);
                }
                reader.Close();
            }
            catch { return null; }

            //get files per slave
            for (int i = 0; i < result.SlaveList.Count; i++)
            {
                Slave sl = result.SlaveList[i];

                cmdStr = " SELECT * FROM SlaveSlotTable " +
                         " WHERE userID = @userID AND slaveID = @slaveID";

                command = new SqlCommand(cmdStr, SQLraw);
                command.Parameters.AddWithValue("@userID", userID);
                command.Parameters.AddWithValue("@slaveID", sl.SlaveID);

                reader = null;
                try
                {
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        SlaveSlot slot = new SlaveSlot();

                        slot.UserID = Convert.ToInt64(reader["userID"]);
                        slot.SlaveID = Convert.ToInt32(reader["slaveID"]);
                        slot.SlotID = Convert.ToInt32(reader["slotID"]);
                        slot.ProgramGroup = Convert.ToInt32(reader["programGroup"]);
                        slot.ProgramSubGroup = Convert.ToInt32(reader["programSubType"]);
                        slot.ProgramVersion = Convert.ToInt32(reader["programVersion"]);
                        sl.SlaveFiles.Add(slot);
                    }
                    reader.Close();
                }
                catch { return null; }
            }

            return result;
        }

        /// <summary>
        /// get server stats
        /// </summary>
        /// <returns></returns>
        public UserStats getStats()
        {
            UserStats result = new UserStats();

            //top10cash
            string cmdStr = " SELECT TOP 10 u.userName AS userName, i.userCash AS cash "+
                            " FROM UserTable u JOIN UserInfo i ON u.userID = i.userID " +
                            " ORDER BY i.userCash DESC";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            
            //get user slaves
            SqlDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    UserStatOne one = new UserStatOne();

                    one.UserName = Convert.ToString(reader["userName"]);
                    one.Amount = Convert.ToInt64(reader["cash"]);
                    result.Top10cash.Add(one);
                }
                reader.Close();
            }
            catch { return null; }

            //top10white
            cmdStr = " SELECT TOP 10 u.userName AS userName, i.userHatPoints AS hats " +
                            " FROM UserTable u JOIN UserInfo i ON u.userID = i.userID " +
                            " ORDER BY i.userHatPoints DESC";

            command = new SqlCommand(cmdStr, SQLraw);

            //get user info
            reader = null;
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    UserStatOne one = new UserStatOne();

                    one.UserName = Convert.ToString(reader["userName"]);
                    one.Amount = Convert.ToInt64(reader["hats"]);
                    result.Top10white.Add(one);
                }
                reader.Close();
            }
            catch { return null; }

            //top10black
            cmdStr = " SELECT TOP 10 u.userName AS userName, i.userHatPoints AS hats " +
                     " FROM UserTable u JOIN UserInfo i ON u.userID = i.userID " +
                     " ORDER BY i.userHatPoints ASC";

            command = new SqlCommand(cmdStr, SQLraw);

            //get user info
            reader = null;
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    UserStatOne one = new UserStatOne();

                    one.UserName = Convert.ToString(reader["userName"]);
                    one.Amount = Convert.ToInt64(reader["hats"]);
                    result.Top10black.Add(one);
                }
                reader.Close();
            }
            catch { return null; }

            //userCount
            cmdStr = " SELECT COUNT(*) AS count " +
                     " FROM UserTable;";

            command = new SqlCommand(cmdStr, SQLraw);

            //get user info
            reader = null;
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result.RegisteredUsers = Convert.ToInt64(reader["count"]);
                }
                reader.Close();
            }
            catch { return null; }

            return result;
        }

        /// <summary>
        /// add default programs to hdd
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public string loadDefaultHDD(string userID)
        {
            string cmdStr = " INSERT INTO UserHDD (userID,hddSlot,programType,programSubType,usesLeft,programVersion)" +
                            " VALUES (@userID , 0 , 1 , 1 , 5, 1), " + //downloader
                            "        (@userID , 1 , 1 , 2 , 5, 1); ";  //uploader

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);
            
            try { command.ExecuteNonQuery(); }
            catch { return "error adding default files"; }

            return "info: saved files to HDD";
        }

        /// <summary>
        /// delete item from hdd
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="slotID"></param>
        /// <returns></returns>
        public string deleteHDDslot(string userID, string slotID)
        {
            string cmdStr = " DELETE FROM UserHDD " +
                            " WHERE userID = @user AND hddSlot = @slot;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@user", userID);
            command.Parameters.AddWithValue("@slot", slotID);

            try { command.ExecuteNonQuery(); }
            catch { return "error emptying slot"; }

            return "emptied hdd slot";
        }

        /// <summary>
        /// delete slave and its files
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="slaveID"></param>
        /// <returns></returns>
        public string deleteSlave(string userID, int slaveID)
        {
            //delete current slave and its files
            string cmdStr = " DELETE FROM SlaveSlotTable " +
                            " WHERE userID = @user AND slaveID = @slaveID; " +
                            " DELETE FROM SlaveTable " +
                            " WHERE userID = @user AND slaveID = @slaveID; ";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@user", userID);
            command.Parameters.AddWithValue("@slaveID", slaveID);

            try { command.ExecuteNonQuery(); }
            catch { return "error: couldn't delete slave"; }

            return "info: slave deleted";
        }

        /// <summary>
        /// delete user mission
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public string deleteMission(string userID)
        {
            //delete slave
            if (deleteSlave(userID, 999).IndexOf("error") != -1)
                return "error: couldn't delete mission slave";

            //delete mission
            string cmdStr = " DELETE FROM UserMission " +
                            " WHERE userID = @user; ";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@user", userID);
            
            try { command.ExecuteNonQuery(); }
            catch { return "error: couldn't delete mission"; }

            return "info: mission deleted";
        }

        /// <summary>
        /// delete item from cpu
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="slotID"></param>
        /// <returns></returns>
        public string deleteCPUslot(string userID, string slotID)
        {
            string cmdStr = " DELETE FROM UserCPU " +
                            " WHERE userID = @user AND cpuSlot = @slot;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@user", userID);
            command.Parameters.AddWithValue("@slot", slotID);

            try { command.ExecuteNonQuery(); }
            catch { return "error emptying slot"; }

            return "emptied cpu slot";
        }

        /// <summary>
        /// delete compilerJob
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="slotID"></param>
        /// <returns></returns>
        public string deleteCompilerJob(string userID)
        {
            string cmdStr = " DELETE FROM CompilerTable " +
                            " WHERE userID = @user;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@user", userID);
            
            try { command.ExecuteNonQuery(); }
            catch { return "error removing job"; }

            return "job removed";
        }

        /// <summary>
        /// debit/credit user cash account
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public string updateUserMoney(string userID, long amount)
        {
            string cmdStr = " UPDATE UserInfo " +
                            " SET userCash = userCash + @amount " +
                            " WHERE userID = @userId ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userId", userID);
            command.Parameters.AddWithValue("@amount", amount);

            try { command.ExecuteNonQuery(); }
            catch { return "error changing user balance"; }

            return "info: transaction successful";
        }

        /// <summary>
        /// debit/credit user gold account
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public string updateUserGold(string userID, long amount)
        {
            string cmdStr = " UPDATE UserInfo " +
                            " SET userGold = userGold + @amount " +
                            " WHERE userID = @userId ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userId", userID);
            command.Parameters.AddWithValue("@amount", amount);

            try { command.ExecuteNonQuery(); }
            catch { return "error changing user balance"; }

            return "info: transaction successful";
        }

        /// <summary>
        /// get hdd size
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public int getHDDsize(string userID)
        {
            int result = -1;

            string cmdStr = "SELECT userHDDslots FROM UserInfo " +
                            "WHERE userID = @userid ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userid", userID);

            SqlDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result = Convert.ToInt32(reader["userHDDslots"]);
                }
                reader.Close();
            }
            catch { }

            return result;
        }

        /// <summary>
        /// get cpu number
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public int getCPUnum(string userID)
        {
            int result = -1;

            string cmdStr = "SELECT userCPUslots FROM UserInfo " +
                            "WHERE userID = @userid ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userid", userID);

            SqlDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result = Convert.ToInt32(reader["userCPUslots"]);
                }
                reader.Close();
            }
            catch { }

            return result;
        }

        /// <summary>
        /// add hdd slot
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public bool addHDDSlot(string userID)
        {
            string cmdStr = " UPDATE UserInfo " +
                            " SET  userHDDslots = userHDDslots + 1 " +
                            " WHERE userID = @userID ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);
            
            try { command.ExecuteNonQuery(); }
            catch { return false; }

            return true;
        }

        /// <summary>
        /// add cpu slot
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public bool addCPUslot(string userID)
        {
            string cmdStr = " UPDATE UserInfo " +
                            " SET  userCPUslots = userCPUslots + 1 " +
                            " WHERE userID = @userID ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);

            try { command.ExecuteNonQuery(); }
            catch { return false; }

            return true;
        }

        /// <summary>
        /// check min client version
        /// </summary>
        /// <param name="serverID"></param>
        /// <returns></returns>
        public string getMinClientVersion(string serverID)
        {
            serverID = serverID.ToLower();

            string cmdStr = "SELECT minClientVersion FROM Server " +
                            "WHERE LOWER(serverID) = @serverID ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@serverID", serverID);

            SqlDataReader reader = null;
            string result = "";
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result = Convert.ToString(reader["minClientVersion"]);
                }
                reader.Close();
            }
            catch { }

            return result;
        }

        /// <summary>
        /// set user info table
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public bool setUserInfo(string userID)
        {
            string cmdStr = " INSERT INTO UserInfo (userID,userHDDslots,userCPUslots,userCash,userGold,userHatPoints)" +
                            " VALUES (@userID , 6 , 4 , 1000 , 5, 0); ";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);

            try { command.ExecuteNonQuery(); }
            catch { return false; }

            return true;
        }

        /// <summary>
        /// add default programs to hdd
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public string setHDDslot(string userID, ProgramHW program)
        {
            string cmdStr = " INSERT INTO UserHDD (userID,hddSlot,programType,programSubType,usesLeft,programVersion)" +
                            " VALUES (@userID , @hddSlot , @programType , @subType , @uses, @version); ";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);
            command.Parameters.AddWithValue("@hddSlot", program.HddSlot);
            command.Parameters.AddWithValue("@programType", program.ProgramType);
            command.Parameters.AddWithValue("@subType", program.ProgramSubType);
            command.Parameters.AddWithValue("@uses", program.UsesLeft);
            command.Parameters.AddWithValue("@version", program.ProgramVersion);

            try { command.ExecuteNonQuery(); }
            catch { return "error saving program"; }

            return "info: saved program to HDD";
        }

        /// <summary>
        /// add program to cpu list
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public string setCPUslot(string userID, ProgramHW program, string slot)
        {
            string cmdStr = " INSERT INTO UserCPU (userID,cpuSlot,programType,programSubType,programVersion)" +
                            " VALUES (@userID , @cpuSlot , @programType , @subType , @version); ";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);
            command.Parameters.AddWithValue("@cpuSlot", slot);
            command.Parameters.AddWithValue("@programType", program.ProgramType);
            command.Parameters.AddWithValue("@subType", program.ProgramSubType);
            command.Parameters.AddWithValue("@version", program.ProgramVersion);

            try { command.ExecuteNonQuery(); }
            catch
            {
                cmdStr = " UPDATE UserCPU " +
                            " SET programType = @programType , programSubType = @programSubType, " +
                            "     programVersion = @programVersion " +
                            " WHERE userID = @userID AND cpuSlot = @cpuSlot;";

                command = new SqlCommand(cmdStr, SQLraw);
                command.Parameters.AddWithValue("@userID", userID);
                command.Parameters.AddWithValue("@cpuSlot", slot);
                command.Parameters.AddWithValue("@programType", program.ProgramType);
                command.Parameters.AddWithValue("@programSubType", program.ProgramSubType);
                command.Parameters.AddWithValue("@programVersion", program.ProgramVersion);

                try { command.ExecuteNonQuery(); }
                catch { return "error saving cpu slot"; }
            }

            return "info: program start successful";
        }

        /// <summary>
        /// set user info table
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public bool setHDDsize(string userID, string driveSize)
        {
            string cmdStr = " UPDATE UserInfo " +
                            " SET  userHDDslots = @driveSize " +
                            " WHERE userID = @userID ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);
            command.Parameters.AddWithValue("@driveSize", driveSize);

            try { command.ExecuteNonQuery(); }
            catch 
            {
                return false;
                //setUserInfo(userID);
            }

            return true;
        }

        /// <summary>
        /// set user info table
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public bool setCPUnum(string userID, string cpuNum)
        {
            string cmdStr = " UPDATE UserInfo " +
                            " SET  userCPUslots = @cpuNum " +
                            " WHERE userID = @userID ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);
            command.Parameters.AddWithValue("@cpuNum", cpuNum);

            try { command.ExecuteNonQuery(); }
            catch
            {
                //setUserInfo(userID);
                return false;
            }

            return true;
        }

        /// <summary>
        /// set user hatPoints
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public bool setUserHatPoints(string userID, int amount)
        {
            string cmdStr = " UPDATE UserInfo " +
                            " SET  userHatPoints = userHatPoints + @amount " +
                            " WHERE userID = @userID ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);
            command.Parameters.AddWithValue("@amount", amount);

            try { command.ExecuteNonQuery(); }
            catch { return false; }

            return true;
        }

        /// <summary>
        /// set compilerJob
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public bool setCompilerJob(string userID, CompilerJob job)
        {
            this.deleteCompilerJob(userID);

            job.StartTime = DateTime.Now.ToUniversalTime().Ticks;
            job.EndTime = ((job.ProgramVersion) * (TimeSpan.TicksPerMinute)) + job.StartTime; 

            string cmdStr = " INSERT INTO CompilerTable (userID,programType,programSubType,programVersion,startTime,endTime,buddyID)" +
                            " VALUES (@userID , @progType , @progSubType , @progVersion , @startTime, @endTime, @buddyID);";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);
            command.Parameters.AddWithValue("@progType", job.ProgramType);
            command.Parameters.AddWithValue("@progSubType", job.ProgramSubType);
            command.Parameters.AddWithValue("@progVersion", job.ProgramVersion);
            command.Parameters.AddWithValue("@startTime", job.StartTime);
            command.Parameters.AddWithValue("@endTime", job.EndTime);
            command.Parameters.AddWithValue("@buddyID", -1);

            try { command.ExecuteNonQuery(); }
            catch { return false; }

            return true;
        }

        /// <summary>
        /// insert userMission into table
        /// </summary>
        /// <param name="mission"></param>
        /// <returns></returns>
        public bool setUserMission(UserMission mission)
        {
            string cmdStr = " INSERT INTO UserMission (userID,missionID,passStrength,needAdmin,havePass,haveAdmin,programGroup,programSubGroup,programVersion)" +
                            " VALUES (@userID , @missionID , @passStrength , @needAdmin , @havePass, @haveAdmin, @programGroup, @programSubGroup, @programVersion);";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", mission.UserID);
            command.Parameters.AddWithValue("@missionID", mission.MissionID);
            command.Parameters.AddWithValue("@passStrength", mission.PassStrength);
            command.Parameters.AddWithValue("@needAdmin", mission.NeedAdmin);
            command.Parameters.AddWithValue("@havePass", mission.HavePass);
            command.Parameters.AddWithValue("@haveAdmin", mission.HaveAdmin);
            command.Parameters.AddWithValue("@programGroup", mission.ProgramGroup);
            command.Parameters.AddWithValue("@programSubGroup", mission.ProgramSubGroup);
            command.Parameters.AddWithValue("@programVersion", mission.ProgramVersion);

            try { command.ExecuteNonQuery(); }
            catch { return false; }

            return true;
        }

        /// <summary>
        /// set slave and its slots
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="slave"></param>
        /// <returns></returns>
        public bool setSlave(string userID, Slave slave)
        {
            //delete slave and its files
            if (deleteSlave(userID, slave.SlaveID).IndexOf("error") != -1)
                return false;

            //add to slaveTable
            string cmdStr = " INSERT INTO SlaveTable (userID,slaveID,endTime,userPass,adminPass)" +
                            " VALUES (@userID , @slaveID , @endTime, @userPass, @adminPass);";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);
            command.Parameters.AddWithValue("@slaveID", slave.SlaveID);
            command.Parameters.AddWithValue("@endTime", slave.EndTime);
            command.Parameters.AddWithValue("@userPass", slave.UserPass);
            command.Parameters.AddWithValue("@adminPass", slave.AdminPass);

            try { command.ExecuteNonQuery(); }
            catch { return false; }

            //insert files
            foreach (SlaveSlot sl in slave.SlaveFiles)
            {
                cmdStr = " INSERT INTO SlaveSlotTable (userID,slaveID,slotID,programGroup,programSubType,programVersion)" +
                         " VALUES (@userID , @slaveID , @slotID, @programGroup, @programSubType, @programVersion);";

                command = new SqlCommand(cmdStr, SQLraw);
                command.Parameters.AddWithValue("@userID", userID);
                command.Parameters.AddWithValue("@slaveID", slave.SlaveID);
                command.Parameters.AddWithValue("@slotID", sl.SlotID);
                command.Parameters.AddWithValue("@programGroup", sl.ProgramGroup);
                command.Parameters.AddWithValue("@programSubType", sl.ProgramSubGroup);
                command.Parameters.AddWithValue("@programVersion", sl.ProgramVersion);

                try { command.ExecuteNonQuery(); }
                catch { return false; }
            }

            return true;
        }

        /// <summary>
        /// set user gold to desired amount
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public bool setGold(string userID, int amount)
        {
            amount = 150;
            string cmdStr = " UPDATE UserInfo " +
                            " SET userGold = @goldAmount " +
                            " WHERE userID = @userID ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@userID", userID);
            command.Parameters.AddWithValue("@goldAmount", amount);

            try { command.ExecuteNonQuery(); }
            catch { return false; }

            return true;
        }

        /// <summary>
        /// check if email is already registered
        /// </summary>
        /// <param name="email">email address</param>
        /// <returns></returns>
        public bool emailExists(string email)
        {
            int result = 0;

            email = email.ToLower();
            string cmdStr = "SELECT COUNT(*) AS count FROM UserTable " +
                            "WHERE LOWER(userEmail) = @email ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@email", email);

            SqlDataReader reader = null;
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    result = Convert.ToInt32(reader["count"]);
                }
                reader.Close();
            }
            catch { }

            return (result == 0 ? false : true);
        }

        /// <summary>
        /// check password
        /// </summary>
        /// <param name="password"></param>
        /// <param name="username"></param>
        /// <returns></returns>
        public bool passwordOK(string password, string username)
        {
            password = password.ToLower();
            username = username.ToLower();

            string cmdStr = "SELECT userPasswordHash FROM UserTable " +
                            "WHERE LOWER(username) = @username ;";

            SqlCommand command = new SqlCommand(cmdStr, SQLraw);
            command.Parameters.AddWithValue("@username", username);

            SqlDataReader reader = null;
            string s_password = "";
            try
            {
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    s_password = Convert.ToString(reader["userPasswordHash"]);
                }
                reader.Close();
            }
            catch { }

            if (s_password.ToLower() == password)
                return true;

            return false;
        }
    }
}