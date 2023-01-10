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
            MySqlConnection conn = new MySqlConnection(connStr);
            try
            {
                Console.WriteLine("Connecting to MySQL...");
                conn.Open();
                //Check for connection
                string sql = "SELECT * FROM chatlog_table";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                MySqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    Console.WriteLine(rdr[0] + " -- " + rdr[1]);
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
	}
}
