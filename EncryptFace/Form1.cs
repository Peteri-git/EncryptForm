using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Encodings.Web;
using System.Security.Cryptography;

namespace EncryptFace
{
    public partial class Form1 : Form
    {
        HubConnection hubConnection;
        List<Room> rooms = new List<Room>();
        string user;
        RSAParameters privateKey;
        public Form1()
        {
            InitializeComponent();
            hubConnection = new HubConnectionBuilder().WithUrl("https://localhost:44392/ChatHub").Build();
            checkBox1.Checked = true;
            listBox1.Enabled = false;
            textBox1.Enabled = false;
            checkBox1.Enabled = false;
            checkBox2.Enabled = false;
            button1.Enabled = false;
            button2.Enabled = false;
        }
        private async void button2_Click(object sender, EventArgs e)
        {
            byte[] key = null;
            byte[] IV = null;
            string choice = "";
            if (checkBox1.Checked == true)
            {
                choice = "AES";
                using (Aes myAES = Aes.Create())
                {
                    key = myAES.Key;
                    IV = myAES.IV;
                }
            }
            if (checkBox2.Checked == true)
            {
                choice = "TripleDES";
                using (TripleDES tripleDES = TripleDES.Create())
                {
                    key = tripleDES.Key;
                    IV = tripleDES.IV;
                }
            }
            await hubConnection.InvokeAsync("AddRoom", textBox1.Text,key,IV,choice);
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            
            hubConnection.On<string,byte[],byte[],string>("AddRoom", (name, key, IV, choice) =>
            {
                listBox1.Items.Add(name);
                rooms.Add(new Room(key, IV, name, choice));
            });
            hubConnection.On<string,string>("Login", (result, user) =>
            {
                if (result == "Success")
                {
                    listBox1.Enabled = true;
                    textBox1.Enabled = true;
                    checkBox1.Enabled = true;
                    checkBox2.Enabled = true;
                    button1.Enabled = true;
                    button2.Enabled = true;
                    textBox2.Enabled = false;
                    textBox3.Enabled = false;
                    button3.Enabled = false;
                    this.user = user;
                }
                if (result == "Failed")
                {
                    this.Text = result;
                }
            });
            hubConnection.On<string,string, byte[], byte[], string>("AcceptInvite", (username,name, key, IV, choice) =>
            {
                if (username == user)
                {
                    byte[] decrypted;
                    using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
                    {
                        rsa.ImportParameters(privateKey);
                        decrypted = rsa.Decrypt(key, false);
                    }
                    listBox1.Items.Add(name);
                    rooms.Add(new Room(decrypted, IV, name, choice));
                }
            });

            await hubConnection.StartAsync();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked==true)
                checkBox2.Checked = false;
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox2.Checked == true)
                checkBox1.Checked = false;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedItem != "")
            {
                foreach (var room in rooms)
                {
                    if (room.name == listBox1.SelectedItem.ToString())
                    {
                        ChatForm chatForm = new ChatForm(room.key, room.IV, room.choice, room.name,user);
                        chatForm.Show();
                    }
                }
            }
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string publicKey;
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider())
            {
                publicKey = rsa.ToXmlString(false);
                privateKey = rsa.ExportParameters(true);
            }
            hubConnection.SendAsync("Login", textBox2.Text, textBox3.Text,publicKey);
        }
        public static byte[] RSADecrypt(byte[] DataToDecrypt, RSAParameters RSAKeyInfo, bool DoOAEPPadding)
        {
            try
            {
                byte[] decryptedData;
                //Create a new instance of RSACryptoServiceProvider.
                using (RSACryptoServiceProvider RSA = new RSACryptoServiceProvider())
                {
                    //Import the RSA Key information. This needs
                    //to include the private key information.
                    RSA.ImportParameters(RSAKeyInfo);

                    //Decrypt the passed byte array and specify OAEP padding.  
                    //OAEP padding is only available on Microsoft Windows XP or
                    //later.  
                    decryptedData = RSA.Decrypt(DataToDecrypt, DoOAEPPadding);
                }
                return decryptedData;
            }
            //Catch and display a CryptographicException  
            //to the console.
            catch (CryptographicException e)
            {
                Console.WriteLine(e.ToString());

                return null;
            }
        }
    }
    public class Room
    {
        public string name { get; set; }
        public string choice { get; set; }
        public byte[] key { get; set; }
        public byte[] IV { get; set; }
        public Room(byte[] key, byte[] IV, string name, string choice)
        {
            this.key = key;
            this.IV = IV;
            this.choice = choice;
            this.name = name;
        }
    }
}
