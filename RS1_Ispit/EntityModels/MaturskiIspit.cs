using System;

namespace RS1_Ispit_asp.net_core.EntityModels
{
    public class MaturskiIspit
    {
        public int Id { get; set; }
        public DateTime Datum { get; set; }
        public int SkolaId { get; set; }
        public Skola Skola { get; set; }
        public int PredmetId { get; set; }
        public Predmet Predmet { get; set; }
        public int NastavnikId { get; set; }
        public Nastavnik Nastavnik { get; set; }
        public string Napomena { get; set; }
    }
}
