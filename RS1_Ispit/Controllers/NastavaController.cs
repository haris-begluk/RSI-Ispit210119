using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RS1_Ispit_asp.net_core.EF;
using RS1_Ispit_asp.net_core.EntityModels;
using RS1_Ispit_asp.net_core.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RS1_Ispit_asp.net_core.Controllers
{
    public class NastavaController : Controller
    {
        private readonly MojContext _context;

        public NastavaController(MojContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var model = _context.Nastavnik.Select(n => new SkolaNastavnikVM
            {
                Nastavnik = n.Ime + " " + n.Prezime,
                NastavnikID = n.Id,
                Skola = _context.Odjeljenje.Include(o => o.Skola).FirstOrDefault(o => o.RazrednikID.Equals(n.Id)).Skola.Naziv


            }).ToList();
            return View(model);
        }
        public IActionResult Odaberi(int id)
        {
            var model = new MaturskiIspitVM
            {
                NastavnikId = id,
                Rows = _context.MaturskiIspit.Include(m => m.Skola).Include(m => m.Predmet)
                .Where(m => m.NastavnikId.Equals(id))
                .Select(m => new Row
                {
                    MaturskiIspitId = m.Id,
                    Datum = m.Datum,
                    Skola = m.Skola.Naziv,
                    Predmet = m.Predmet.Naziv,
                    NisuPristupili = _context.MaturskiIspitStavka.Include(mis => mis.Ucenik)
                .Where(mis => mis.MaturskiIspitId.Equals(m.Id) && mis.PristupioIspitu.Equals(false))
                .Select(u => u.Ucenik.ImePrezime).ToList()
                }).ToList()
            };

            return View(model);
        }
        public IActionResult Dodaj(int id)
        {
            var model = new MaturskiIspitDodajVM
            {
                Skole = _context.Skola.Select(s => new SelectListItem
                {
                    Value = s.Id.ToString(),
                    Text = s.Naziv
                }).ToList(),
                NastavnikId = id,
                Nastavnik = _context.Nastavnik
                .Where(n => n.Id.Equals(id))
                .Select(s => s.Ime + " " + s.Prezime)
                .FirstOrDefault(),
                SkolskaGodina = _context.Odjeljenje.Include(o => o.SkolskaGodina)
                .Where(o => o.RazrednikID.Equals(id))
                .Select(o => o.SkolskaGodina.Aktuelna ? o.SkolskaGodina.Naziv : (DateTime.Now.Year + "/" + (DateTime.Now.Year + 1))).
                FirstOrDefault(),
                Predmet = _context.PredajePredmet.Include(p => p.Predmet)
                .Where(p => p.NastavnikID.Equals(id))
                .Select(p => new SelectListItem
                {
                    Value = p.Predmet.Id.ToString(),
                    Text = p.Predmet.Naziv
                }).ToList()

            };

            return View(model);
        }
        public IActionResult Snimi(MaturskiIspitDodajVM item)
        {
            var maturskiIspit = new MaturskiIspit // PROVJERI KAKO RADI
            {
                NastavnikId = item.NastavnikId,
                SkolaId = item.OdabranaSkola,
                PredmetId = item.OdabraniPredmet,
                Napomena = "",
                Datum = item.DatumIspita
            };
            _context.MaturskiIspit.Add(maturskiIspit);
            int odjeljenjeId = _context.Odjeljenje.Where(o => o.Razred.Equals(4) && o.SkolaID.Equals(item.OdabranaSkola)).FirstOrDefault().Id;
            var ucenici = _context.OdjeljenjeStavka
                .Include(o => o.Ucenik)
                .Where(u => u.OdjeljenjeId
                .Equals(odjeljenjeId))
                .Select(o => o.Ucenik)
                .ToList();
            foreach (var ucenik in ucenici)
            {
                bool status = ProvjeriUcenika(ucenik.Id);
                if (status)
                {
                    var maturskiIspitStavka = new MaturskiIspitStavka
                    {
                        MaturskiIspitId = maturskiIspit.Id,
                        PristupioIspitu = false,
                        ProsjekOcjena = _context.DodjeljenPredmet.Where(d => d.OdjeljenjeStavkaId.Equals(
                            _context.OdjeljenjeStavka.Where(o => o.UcenikId.Equals(ucenik.Id)).FirstOrDefault().Id)).Average(b => b.ZakljucnoKrajGodine),
                        Rezultat = 0,
                        UcenikId = ucenik.Id
                    };
                    _context.MaturskiIspitStavka.Add(maturskiIspitStavka);
                }
            }
            _context.SaveChanges();

            return Redirect("/Nastava/Odaberi?Id=" + item.NastavnikId);
        }

        public IActionResult Uredi(int id)
        {
            var model = _context.MaturskiIspit.Include(m => m.Predmet).Where(m => m.Id.Equals(id))
                .Select(c => new MaturskiIspitUrediVM
                {
                    MaturskiIspitId = c.Id,
                    Predmet = c.Predmet.Naziv,
                    Datum = c.Datum.ToString("yyyy-MM-dd"),
                    Napomena = c.Napomena
                }).FirstOrDefault();
            return View(model);
        }
        public IActionResult UrediSnimi(MaturskiIspitUrediVM obj)
        {
            var maturskiIspit = _context.MaturskiIspit.FirstOrDefault(m => m.Id.Equals(obj.MaturskiIspitId));
            maturskiIspit.Napomena = obj.Napomena;
            //_context.MaturskiIspit.Update(maturskiIspit);
            _context.SaveChanges();
            return Redirect("/Nastava/Uredi?id=" + obj.MaturskiIspitId);
        }
        public IActionResult AjaxStavke(int id)
        {
            var model = _context.MaturskiIspitStavka.Include(m => m.Ucenik)
                .Where(m => m.MaturskiIspitId.Equals(id))
                .Select(a => new AjaxStavkeVM
                {
                    MaturskiIspitStavkaId = id,
                    Ucenik = a.Ucenik.ImePrezime,
                    ProsjekOcjena = _context.DodjeljenPredmet
                .Where(o => o.OdjeljenjeStavkaId.Equals(_context.OdjeljenjeStavka
                .Where(s => s.UcenikId.Equals(a.UcenikId)).FirstOrDefault().Id)).Average(o => o.ZakljucnoKrajGodine),
                    PristupioIspitu = a.PristupioIspitu ? "DA" : "NE",
                    Rezulat = a.Rezultat

                }).ToList();
            return PartialView(model);
        }
        public IActionResult UrediUcenika(int id)
        {
            var stavka = _context.MaturskiIspitStavka.Include(m => m.Ucenik)
            .FirstOrDefault(u => u.Id.Equals(id));
            var model = new UrediUcenikaVM
            {
                MaturskiIspitStavkaId = stavka.Id,
                Ucenik = stavka.Ucenik.ImePrezime,
                Bodovi = stavka.Rezultat
            };
            return PartialView(model);
        }
        public IActionResult UrediUcenikaSnimi(UrediUcenikaVM obj)
        {
            var maturskiIspitStavka = _context.MaturskiIspitStavka.FirstOrDefault(m => m.Id.Equals(obj.MaturskiIspitStavkaId));

            maturskiIspitStavka.Rezultat = obj.Bodovi;
            _context.SaveChanges();
            return Redirect("/Nastava/Uredi?id=" + maturskiIspitStavka.MaturskiIspitId);
        }
        public IActionResult Pristupio(int id)
        {
            var stavka = _context.MaturskiIspitStavka.FirstOrDefault(m => m.Id.Equals(id));
            stavka.PristupioIspitu = !stavka.PristupioIspitu;
            _context.SaveChanges();
            Task.Delay(2000).Wait();
            return Redirect("/Nastava/Uredi?id=" + stavka.MaturskiIspitId);
        }
        public bool ProvjeriUcenika(int id) //PROVJERI KAKO RADI
        {
            bool status = true;
            if (_context.DodjeljenPredmet
                .Where(g => g.OdjeljenjeStavkaId == _context
                .OdjeljenjeStavka.Where(h => h.UcenikId == id).FirstOrDefault().Id).Count(x => x.ZakljucnoKrajGodine == 1) > 0)
            {
                status = false;
            }

            if (_context.MaturskiIspitStavka.Where(x => x.UcenikId == id).Count(g => g.Rezultat > 55) > 0)
            {
                status = false;
            }

            return status;
        }
    }
}

