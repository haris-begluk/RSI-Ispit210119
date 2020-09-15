namespace RS1_Ispit_asp.net_core.EntityModels
{
    public class MaturskiIspitStavka
    {
        public int Id { get; set; }
        public int MaturskiIspitId { get; set; }
        public MaturskiIspit MaturskiIspit { get; set; }
        public int UcenikId { get; set; }
        public Ucenik Ucenik { get; set; }
        public bool PristupioIspitu { get; set; }
        public double ProsjekOcjena { get; set; }
        public float Rezultat { get; set; }

    }
}
