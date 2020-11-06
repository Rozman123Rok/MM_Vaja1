using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using iTextSharp.text.pdf.codec;

namespace Test
{
    public partial class Form1 : Form
    {

        int izbrali_BP = 0, izbrali_CT = 0; // ce so ze vnesli
        byte[,] barve = new byte[256, 3]; // BP
        short[,] slika = new short[513, 513];  // slika ki si jo bom shranil
        Bitmap moja_slika = new Bitmap(513, 513); // da ustvarim sliko
        int T = 0; // T vrednost kot koliko je lahko napake
        BinaryReader br; // za branje datotek
        int img_size = 512; // velikost slike


        String string1 = ""; // to so deli niza
        String string2 = "";
        String string3 = "";
        String string4 = "";


        int Zacetek = 0, Konec = 512; // zacetek pa konec pri kompresiji
        string niz = ""; // niz ki ga izpisem


        int bits = 12; // koliko bitov rabim
        short vrednost = 0;
        int index;
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // nalozi BP
            OpenFileDialog dialog = new OpenFileDialog();

            if (dialog.ShowDialog() == DialogResult.OK)
            {

                br = new BinaryReader(File.Open(dialog.FileName, FileMode.Open));

                for (int i = 0; i < 256; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        barve[i, j] = br.ReadByte();
                    }
                }
            }

            izbrali_BP = 1;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // NALOZI CT
            OpenFileDialog dialog = new OpenFileDialog(); // odprem fileDialog da lahko izberem datoteko

            if (dialog.ShowDialog() == DialogResult.OK) // ce je ok rezultat
            {
                br = new BinaryReader(File.Open(dialog.FileName, FileMode.Open)); // odprem datoteko
                for (int i = 0; i < img_size; i++)
                {
                    for (int j = 0; j < img_size; j++)
                    {
                        slika[i, j] = br.ReadInt16(); // si jo shranim v sliko
                    }
                }
            }

            izbrali_CT = 1; // da sem nalozil sliko
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // PRIKAZI SLIKO
            if (izbrali_BP == 1 && izbrali_CT == 1)
            {
                for (int i = 0; i < 512; i++)
                {
                    for (int j = 0; j < 512; j++)
                    {
                        double temp = ((slika[i, j] + 2048.0) / 4095.0) * 255.0;
                        int a = slika[i, j];

                        moja_slika.SetPixel(i, j, Color.FromArgb(barve[(int)temp, 0], barve[(int)temp, 1], barve[(int)temp, 2]));
                    }
                }

                pictureBox1.Image = moja_slika;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // KOMPRESIRAJ
            T = int.Parse(textBox1.Text); // pridobim T
            int polovica = Konec / 2; // kje je polovica izracunam
            //System.Windows.MessageBox.Show("asd" + polovica);

            // si pripravim 4 nize ki so samo deli niza
            string1 = "";
            string2 = "";
            string3 = "";
            string4 = "";

            // naredim 4 taske da grejo razdelit sliko
            Task n1 = Task.Factory.StartNew(() => razdeliSliko(Zacetek, Zacetek, polovica, 1)); // levo zgoraj (0,0)
            Task n2 = Task.Factory.StartNew(() => razdeliSliko(Zacetek + polovica, Zacetek, polovica, 2)); // desno zgoraj (polovica,0) 
            Task n3 = Task.Factory.StartNew(() => razdeliSliko(Zacetek, Zacetek + polovica, polovica, 3)); // levo spodaj (0, polovica)
            Task n4 = Task.Factory.StartNew(() => razdeliSliko(Zacetek + polovica, Zacetek + polovica, polovica, 4)); // desno spodaj (polovica, polovica)

            //pocakam da se vsi taski zaklucijo
            Task.WaitAll(n1, n2, n3, n4);

            niz += string1 + string2 + string3 + string4; // zdruzim stringe v en niz
            niz = Convert.ToString(9, 2).PadLeft(8, '0') + niz;

            
            Zapisi_v_Dat(); // zapisem v dat
            System.Windows.MessageBox.Show("KONCANO");

        }

        private void button5_Click(object sender, EventArgs e)
        {
            string text = File.ReadAllText(@"out.txt");
            niz = text; // shranim prebrano v niz
            dek();
            System.Windows.MessageBox.Show("KONCANO");
            izbrali_CT = 1; // ker ko je dekompresije konec je isto koda sem si nalozil sliko

        }
        private void razdeliSliko(int s1, int s2, int velikost, int id)
        {// s1 in s2 sta zacetni koordinati , velikost je koliko jih je

            int polovica; // koliko je polovica
            bool lahkoKomp = true; // ce so vsi enake vrednosti
            short prvi = slika[s1, s2]; // vzamem prvi element ki ga bom primerjal
            for (int i = s1; i < s1 + velikost; i++)
            {
                for (int j = s2; j < s2 + velikost; j++)
                {
                    if (Math.Abs(slika[i, j] - prvi) > T)
                    {
                        lahkoKomp = false; // se ne ujema moremo razdelit
                    }
                }
            }

            if (lahkoKomp)
            {
                // potem lahko zapisujem v niz
                if (prvi < 0)
                {
                    // ima negativno vrednost
                    /// -2048 bit je 1000 0000 0000 in more 0 bit spredaj
                    switch (id)
                    {
                        // pogledam keri id ma thread
                        case 1:
                            string1 += "01" + Convert.ToString((short)(prvi * -1), 2).PadLeft(12, '0').Substring(1, 11);
                            break;
                        case 2:
                            string2 += "01" + Convert.ToString((short)(prvi * -1), 2).PadLeft(12, '0').Substring(1, 11);
                            break;
                        case 3:
                            string3 += "01" + Convert.ToString((short)(prvi * -1), 2).PadLeft(12, '0').Substring(1, 11);
                            break;
                        case 4:
                            string4 += "01" + Convert.ToString((short)(prvi * -1), 2).PadLeft(12, '0').Substring(1, 11);
                            break;
                    }
                }
                else
                {
                    //2047 = 0111 1110 1011 in 0 spredaj
                    switch (id)
                    {
                        case 1:
                            string1 += "00" + Convert.ToString(prvi, 2).PadLeft(12, '0').Substring(1, 11);
                            break;
                        case 2:
                            string2 += "00" + Convert.ToString(prvi, 2).PadLeft(12, '0').Substring(1, 11);
                            break;
                        case 3:
                            string3 += "00" + Convert.ToString(prvi, 2).PadLeft(12, '0').Substring(1, 11);
                            break;
                        case 4:
                            string4 += "00" + Convert.ToString(prvi, 2).PadLeft(12, '0').Substring(1, 11);
                            break;
                    }
                }
            }
            else
            {
                // morem se naprej delit zapisem 1
                // in v string si shranim 1
                switch (id)
                {
                    case 1:
                        string1 += "1";
                        break;
                    case 2:
                        string2 += "1";
                        break;
                    case 3:
                        string3 += "1";
                        break;
                    case 4:
                        string4 += "1";
                        break;
                }

                // ponovno vse 4 delitve
                polovica = (velikost / 2);
                razdeliSliko(s1, s2, polovica, id);
                polovica = (velikost / 2);
                razdeliSliko(s1 + polovica, s2, polovica, id);
                polovica = (velikost / 2);
                razdeliSliko(s1, s2 + polovica, polovica, id);
                polovica = (velikost / 2);
                razdeliSliko(s1 + polovica, s2 + polovica, polovica, id);
            }

        }

        private void Zapisi_v_Dat()
        {
            // zapisem v datoteko
            File.WriteAllText(@"out.txt", niz);
        }

        private void dek() {
            int n = Convert.ToInt32(niz.Substring(index, 8), 2);
            //Console.WriteLine("n: " + n);

            Zacetek = 0;
            Konec = (int)Math.Pow(2, n); // kako velika je slika 
            int polovica = Konec / 2; // jo damo na polovico

            // rekurzivno delim sliko na 4 dele
            slika = new short[Konec, Konec]; // naredim novo polje za sliko
            index = 8;
            delitevDek(Zacetek, Zacetek, polovica);
            delitevDek(Zacetek + polovica, Zacetek, polovica);
            delitevDek(Zacetek, Zacetek + polovica, polovica);
            delitevDek(Zacetek + polovica, Zacetek + polovica, polovica);
        }

        private void delitevDek(int s1, int s2, int velikost) {
            if (niz[index] == '1')
            {
                // ce je 1 smo naprej delili
                int pol = (velikost / 2);
                index++;
                delitevDek(s1, s2, pol);
                delitevDek(s1 + pol, s2, pol);
                delitevDek(s1, s2 + pol, pol);
                delitevDek(s1 + pol, s2 + pol, pol);
            }
            else
            {
                // 0 torej imamo eno barvo
                //preberem 12 bitov in zapišem to barvo not 
                index++;
                if (niz[index] == '1')
                    vrednost = -1;
                else
                    vrednost = 1;

                index++;

                if ((short)Convert.ToInt32(niz.Substring(index, bits - 1), 2) == 0 && vrednost == -1)
                    vrednost = -2048;
                else
                    vrednost *= (short)Convert.ToInt32(niz.Substring(index, 11), 2);



                for (int i = s1; i < s1 + velikost; i++)
                {
                    for (int j = s2; j < s2 + velikost; j++)
                    {
                        slika[i, j] = vrednost; // si shranimo v sliko
                    }
                }

                index += (bits - 1);
            }
        }

    }
}
