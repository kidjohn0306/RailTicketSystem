using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data;

namespace RailTicketSystem
{
    public partial class MyPageForm : Form
    {
        private TextBox txtName, txtPhone, txtCard;
        private Button btnUpdate, btnDelete;
        DBHelper db = new DBHelper();

        public MyPageForm()
        {
            InitializeCustomUI();
            LoadMyInfo();
        }

        private void InitializeCustomUI()
        {
            this.Text = "내 정보 관리";
            this.Size = new Size(400, 350);
            this.StartPosition = FormStartPosition.CenterParent;


            int xLabel = 30;
            int xText = 120;
            int txtWidth = 200;
            int yStart = 40;
            int step = 50;

            // 1. 이름
            Label lblName = new Label() { Text = "이름", Location = new Point(xLabel, yStart), AutoSize = true };
            txtName = new TextBox() { Location = new Point(xText, yStart - 3), Width = txtWidth };
            this.Controls.Add(lblName); this.Controls.Add(txtName);

            // 2. 전화번호
            Label lblPhone = new Label() { Text = "전화번호", Location = new Point(xLabel, yStart + step), AutoSize = true };
            txtPhone = new TextBox() { Location = new Point(xText, yStart + step - 3), Width = txtWidth };
            this.Controls.Add(lblPhone); this.Controls.Add(txtPhone);

            // 3. 카드번호
            Label lblCard = new Label() { Text = "카드번호", Location = new Point(xLabel, yStart + step * 2), AutoSize = true };
            txtCard = new TextBox() { Location = new Point(xText, yStart + step * 2 - 3), Width = txtWidth };
            this.Controls.Add(lblCard); this.Controls.Add(txtCard);

            // 4. 버튼
            int btnY = yStart + step * 3 + 20;

            btnUpdate = new Button() { Text = "정보 수정", Location = new Point(50, btnY), Size = new Size(120, 40), BackColor = Color.LightSkyBlue };
            btnUpdate.Click += BtnUpdate_Click;
            this.Controls.Add(btnUpdate);

            btnDelete = new Button() { Text = "회원 탈퇴", Location = new Point(200, btnY), Size = new Size(120, 40), BackColor = Color.LightPink };
            btnDelete.Click += BtnDelete_Click;
            this.Controls.Add(btnDelete);
        }

        // 내 정보 불러오기
        private void LoadMyInfo()
        {
            try
            {
                string id = Form1.CurrentUserID;
                string sql = $"SELECT 회원이름, 휴대전화, 카드번호 FROM 회원 WHERE 회원번호 = '{id}'";
                var dt = db.GetDataTable(sql);

                if (dt.Rows.Count > 0)
                {
                    txtName.Text = dt.Rows[0]["회원이름"].ToString();
                    txtPhone.Text = dt.Rows[0]["휴대전화"].ToString();
                    txtCard.Text = dt.Rows[0]["카드번호"].ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("정보 불러오기 오류: " + ex.Message);
            }
        }

        // 수정 버튼
        private void BtnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                string sql = $@"
                    UPDATE 회원 
                    SET 회원이름 = '{txtName.Text}', 
                        휴대전화 = '{txtPhone.Text}', 
                        카드번호 = '{txtCard.Text}' 
                    WHERE 회원번호 = '{Form1.CurrentUserID}'";

                db.ExecuteQuery(sql);
                MessageBox.Show("회원 정보가 수정되었습니다.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("수정 실패: " + ex.Message);
            }
        }

        // 탈퇴 버튼
        private void BtnDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("정말 탈퇴하시겠습니까?\n모든 예매 내역도 함께 삭제됩니다.", "탈퇴 경고", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                try
                {
                    // 회원 삭제 
                    string id = Form1.CurrentUserID;

                    // 1. 내 예약 삭제
                    db.ExecuteQuery($"DELETE FROM 예약현황 WHERE 회원번호 = '{id}'");

                    // 2. 회원 삭제
                    string sql = $"DELETE FROM 회원 WHERE 회원번호 = '{id}'";
                    db.ExecuteQuery(sql);

                    MessageBox.Show("탈퇴되었습니다. 프로그램을 종료합니다.");
                    Application.Exit();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("탈퇴 오류: " + ex.Message);
                }
            }
        }
    }
}