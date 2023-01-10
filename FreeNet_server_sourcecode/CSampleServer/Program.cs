using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using FreeNet;
using MySql.Data.MySqlClient;

namespace CSampleServer
{
	class Program
	{
        public static List<string> chatlog_list = new List<string>();
        static MySqlConnection conn;
		static List<CGameUser> userlist;

        static void Main(string[] args)
		{
			CPacketBufferManager.initialize(2000);
			userlist = new List<CGameUser>();

			CNetworkService service = new CNetworkService();
			// 콜백 매소드 설정.
			service.session_created_callback += on_session_created;
			// 초기화.
			service.initialize();

			var host = Dns.GetHostEntry(Dns.GetHostName());
			string local_IP = "";

			foreach(var ip in host.AddressList)
            {
				if(ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
					local_IP = ip.ToString();
					break;
				}
            }

            //Access mysql database. And check if the connection is successful.
            string connStr = "server=localhost;user=root;database=ChatLog;port=3306;password=vhzptapahflA123";
            conn = new MySqlConnection(connStr);
			
            try
            {
                Console.WriteLine("Connecting to MySQL...");
                conn.Open();
				
                //Check for connection
                if (conn.State == System.Data.ConnectionState.Open)
                {
                    Console.WriteLine("Connection is successful.");
                }
                else
                {
                    Console.WriteLine("Connection is failed.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            //service.listen("127.0.0.1", 7979, 100); // IP를 직접 입력하는 방식
            Console.WriteLine(string.Format("Get Local IP -> {0}", local_IP)); // 현재 컴퓨터의 IP 주소를 가져오는 방식
			service.listen(local_IP, 7979, 100); // 포트는 7979로 고정

			Console.WriteLine("Started!");
			while (true)
			{
				//Console.Write(".");
				System.Threading.Thread.Sleep(1000);
			}

			Console.ReadKey();
		}

		/// <summary>
		/// 클라이언트가 접속 완료 하였을 때 호출됩니다.
		/// n개의 워커 스레드에서 호출될 수 있으므로 공유 자원 접근시 동기화 처리를 해줘야 합니다.
		/// </summary>
		/// <returns></returns>
		static void on_session_created(CUserToken token)
		{
			CGameUser user = new CGameUser(token);
			user.callback_get_tokenlist += GetTokenList;

			lock (userlist)
			{
				userlist.Add(user);
			}
		}

		/// <summary>
		/// 클라이언트가 접속 해제를 하였을 때 호출됩니다.
		/// </summary>
		/// <param name="user"></param>
		public static void remove_user(CGameUser user)
		{
			lock (userlist)
			{
				userlist.Remove(user);
			}
		}

		/// <summary>
		/// 서버에 접속한 클라이언트 토큰 리스트를 반환한다.
		/// </summary>
		/// <returns>클라이언트 토큰 리스트</returns>
		public static List<CGameUser> GetTokenList()
        {
			return userlist;
        }

		/// <summary>
		/// 지금까지 기록된 채팅 로그를 데이터베이스에 저장한다.
		/// </summary>
		public static void MySqlSaveData()
		{
            //The data in the chatlog_list list are stored in the chatlog_table table in the order in which they are listed. When saving a table, data is stored in the chatLog_Message column.
            string sql = "INSERT INTO chatlog_table(chatLog_Message) VALUES(@chatLog_Message)";
            MySqlCommand cmd = new MySqlCommand(sql, conn);

            foreach (string chatlog in chatlog_list)
            {
                cmd.Parameters.AddWithValue("@chatLog_Message", chatlog);
                cmd.ExecuteNonQuery();
            }

            chatlog_list.Clear();

            Console.WriteLine("Data is saved.");
        }

        /// <summary>
        /// 데이터베이스에 저장된 채팅 로그를 가져온다.
        /// </summary>
        /// <returns>저장된 채팅 로그</returns>
        public static List<string> MySqlGetData()
		{
            //If there is data in the chatlog_table table from the Mysql ChatLog data, only the chatLog_Message column is taken and returned in a string-type list. Returns null if there is no data in the table.
            List<string> chatLogList = new List<string>();
            string sql = "SELECT chatLog_Message FROM chatlog_table";
            MySqlCommand cmd = new MySqlCommand(sql, conn);
            MySqlDataReader rdr = cmd.ExecuteReader();

            while (rdr.Read())
            {
                chatLogList.Add(rdr[0].ToString());
            }

            rdr.Close();

            return chatLogList;
        }
    }
}
