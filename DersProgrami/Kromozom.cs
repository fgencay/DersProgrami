using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DersProgrami
{
    public class Kromozom
    {
        public List<DersAtamasi> Atamalar { get; set; }
        public int Fitness { get; set; }
        public int CakismaSayisi { get; set; }
        public int BoslukCezasi { get; set; }

        public Kromozom Kopyala()
        {
            return new Kromozom
            {
                Atamalar = Atamalar.Select(a => new DersAtamasi
                {
                    Ders = new Ders { Ad = a.Ders.Ad, Ogretmen = a.Ders.Ogretmen,
                        Grup = a.Ders.Grup, Sure = a.Ders.Sure, Sinif = a.Ders.Sinif },
                    Slot = new ZamanSlotu { Gun = a.Slot.Gun, Saat = a.Slot.Saat },
                    Sinif = a.Sinif
                }).ToList(),
                Fitness = Fitness, CakismaSayisi = CakismaSayisi, BoslukCezasi = BoslukCezasi
            };
        }

        public Kromozom()
        {
            Atamalar = new List<DersAtamasi>();
        }
    }
}
