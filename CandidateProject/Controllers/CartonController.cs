using CandidateProject.EntityModels;
using CandidateProject.ViewModels;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace CandidateProject.Controllers
{
    public class CartonController : Controller
    {
        private CartonContext db = new CartonContext();

        // GET: Carton
        public ActionResult Index()
        {
            var cartons = db.Cartons
                .Select(c => new CartonViewModel
                {
                    Id = c.Id,
                    CartonNumber = c.CartonNumber,
                    EquipmentCount = c.CartonDetails.Count // Add count of equipment
                })
                .ToList();

            return View(cartons);
        }


        // GET: Carton/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var carton = db.Cartons
                .Where(c => c.Id == id)
                .Select(c => new CartonViewModel()
                {
                    Id = c.Id,
                    CartonNumber = c.CartonNumber
                })
                .SingleOrDefault();
            if (carton == null)
            {
                return HttpNotFound();
            }
            return View(carton);
        }

        // GET: Carton/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Carton/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Id,CartonNumber")] Carton carton)
        {
            if (ModelState.IsValid)
            {
                db.Cartons.Add(carton);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(carton);
        }

        // GET: Carton/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var carton = db.Cartons
                .Where(c => c.Id == id)
                .Select(c => new CartonViewModel()
                {
                    Id = c.Id,
                    CartonNumber = c.CartonNumber
                })
                .SingleOrDefault();
            if (carton == null)
            {
                return HttpNotFound();
            }
            return View(carton);
        }

        // POST: Carton/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "Id,CartonNumber")] CartonViewModel cartonViewModel)
        {
            if (ModelState.IsValid)
            {
                var carton = db.Cartons.Find(cartonViewModel.Id);
                carton.CartonNumber = cartonViewModel.CartonNumber;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(cartonViewModel);
        }

        // GET: Carton/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Carton carton = db.Cartons.Find(id);
            if (carton == null)
            {
                return HttpNotFound();
            }
            return View(carton);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var carton = db.Cartons.Include(c => c.CartonDetails).SingleOrDefault(c => c.Id == id);

            if (carton == null)
            {
                return HttpNotFound();
            }

            // Remove all associated CartonDetails
            db.CartonDetails.RemoveRange(carton.CartonDetails);

            // Remove the Carton
            db.Cartons.Remove(carton);
            db.SaveChanges();

            return RedirectToAction("Index");
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        public ActionResult AddEquipment(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var carton = db.Cartons
                .Where(c => c.Id == id)
                .Select(c => new CartonDetailsViewModel()
                {
                    CartonNumber = c.CartonNumber,
                    CartonId = c.Id
                })
                .SingleOrDefault();

            if (carton == null)
            {
                return HttpNotFound();
            }

            // Check if the carton is full
            bool isCartonFull = db.CartonDetails.Count(cd => cd.CartonId == id) >= 10;

            // Fetch equipment with their assignment status
            var equipment = db.Equipments
                .Select(e => new EquipmentViewModel()
                {
                    Id = e.Id,
                    ModelType = e.ModelType.TypeName,
                    SerialNumber = e.SerialNumber,
                    IsAssigned = db.CartonDetails.Any(cd => cd.EquipmentId == e.Id) // Check assignment
                })
                .ToList();

            carton.Equipment = equipment;
            carton.IsFull = isCartonFull; // Pass carton capacity status to the view

            return View(carton);
        }


        public ActionResult AddEquipmentToCarton([Bind(Include = "CartonId,EquipmentId")] AddEquipmentViewModel addEquipmentViewModel)
        {
            if (ModelState.IsValid)
            {
                // Check if equipment is already in another carton
                bool isEquipmentAssigned = db.CartonDetails.Any(cd => cd.EquipmentId == addEquipmentViewModel.EquipmentId);
                if (isEquipmentAssigned)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Equipment is already assigned to another carton.");
                }

                var carton = db.Cartons.Include(c => c.CartonDetails).SingleOrDefault(c => c.Id == addEquipmentViewModel.CartonId);

                if (carton == null)
                {
                    return HttpNotFound();
                }

                // Ensure the carton does not exceed the capacity limit
                if (carton.CartonDetails.Count >= 10)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Carton has reached its maximum capacity of 10 items.");
                }

                var equipment = db.Equipments.SingleOrDefault(e => e.Id == addEquipmentViewModel.EquipmentId);
                if (equipment == null)
                {
                    return HttpNotFound();
                }

                var detail = new CartonDetail
                {
                    Carton = carton,
                    Equipment = equipment
                };

                carton.CartonDetails.Add(detail);
                db.SaveChanges();
            }

            return RedirectToAction("AddEquipment", new { id = addEquipmentViewModel.CartonId });
        }



        public ActionResult ViewCartonEquipment(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            var carton = db.Cartons
                .Where(c => c.Id == id)
                .Select(c => new CartonDetailsViewModel()
                {
                    CartonNumber = c.CartonNumber,
                    CartonId = c.Id,
                    Equipment = c.CartonDetails
                        .Select(cd => new EquipmentViewModel()
                        {
                            Id = cd.EquipmentId,
                            ModelType = cd.Equipment.ModelType.TypeName,
                            SerialNumber = cd.Equipment.SerialNumber
                        })
                })
                .SingleOrDefault();
            if (carton == null)
            {
                return HttpNotFound();
            }
            return View(carton);
        }



        public ActionResult RemoveEquipmentOnCarton([Bind(Include = "CartonId,EquipmentId")] RemoveEquipmentViewModel removeEquipmentViewModel)
        {
            if (ModelState.IsValid)
            {
                var cartonDetail = db.CartonDetails
                    .Where(cd => cd.CartonId == removeEquipmentViewModel.CartonId && cd.EquipmentId == removeEquipmentViewModel.EquipmentId)
                    .SingleOrDefault();

                if (cartonDetail == null)
                {
                    return HttpNotFound("Equipment not found in the carton.");
                }

                db.CartonDetails.Remove(cartonDetail);
                db.SaveChanges();

                return RedirectToAction("ViewCartonEquipment", new { id = removeEquipmentViewModel.CartonId });
            }

            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid data provided.");
        }

        public ActionResult RemoveAllEquipment(int id)
        {
            var carton = db.Cartons.Include(c => c.CartonDetails).SingleOrDefault(c => c.Id == id);

            if (carton == null)
            {
                return HttpNotFound();
            }

            db.CartonDetails.RemoveRange(carton.CartonDetails);
            db.SaveChanges();

            return RedirectToAction("ViewCartonEquipment", new { id = id });
        }


    }
}
