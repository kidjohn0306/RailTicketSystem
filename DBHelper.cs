using System;
using System.Data;
using MySql.Data.MySqlClient;
using System.Windows.Forms;

namespace RailTicketSystem
{
    public class DBHelper
    {
        // DB 접속 문자열 정의
        private string connectionString = "Server=127.0.0.1;Database=RailDB;Uid=root;Pwd=1234;CharSet=utf8;";

        // 1. 쿼리 실행용 (INSERT, UPDATE, DELETE) - 결과가 필요 없을 때
        public void ExecuteQuery(string query)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    MySqlCommand cmd = new MySqlCommand(query, conn);
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {   // 오류 발생 시 사용자에게 알림 (Try-Catch로 오류 억제)
                MessageBox.Show("DB 오류: " + ex.Message);
            }
        }

        // 2. 데이터 조회용 (SELECT) - 결과표(DataTable)를 가져올 때
        public DataTable GetDataTable(string query)
        {
            DataTable dt = new DataTable();
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    MySqlDataAdapter da = new MySqlDataAdapter(query, conn);
                    da.Fill(dt);
                }
            }
            catch (Exception ex)
            {   // 조회 오류 발생 시에도 프로그램은 계속 실행되도록 예외를 억제하고 빈 테이블 반환하도록 했음
                MessageBox.Show("DB 조회 오류: " + ex.Message);
            }
            return dt;
        }
    }
}