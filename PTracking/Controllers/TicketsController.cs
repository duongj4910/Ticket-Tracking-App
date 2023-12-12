﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PTracking.Data;
using PTracking.Models;
using PTracking.ViewModel;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static NuGet.Packaging.PackagingConstants;

namespace PTracking.Controllers
{
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public TicketsController(ApplicationDbContext context)
        {
            _context = context;
        }

		public async Task<IActionResult> Index()
		{
			// Retrieve tickets data
			var tickets = await _context.Tickets.ToListAsync();

			// Retrieve other necessary data
			int activeTicketCount = _context.Tickets.Count(ticket => ticket.Status == "Incomplete");
			// Other data retrieval logic...

			// Store data in ViewBag
			ViewBag.Tickets = tickets;
			ViewBag.ActiveTicketCount = activeTicketCount;

			// Get completion data
			int completeCount = _context.Tickets.Count(p => p.Status == "Completed");
			int incompleteCount = _context.Tickets.Count(p => p.Status == "Incomplete");
			int inProgressCount = _context.Tickets.Count(p => p.Status == "In Progress");

			// Store completion data in a structured object
			var completionData = new
			{
				Labels = new List<string> { "Complete", "Incomplete", "In Progress" },
				TicketCounts = new List<int> { completeCount, incompleteCount, inProgressCount }
			};

			// Assign the structured data to ViewBag
			ViewBag.ChartLabels = completionData.Labels; // Use ViewBag.ChartLabels instead of ViewBag.CompletionData
			ViewBag.ChartData = completionData.TicketCounts; // Use ViewBag.ChartData instead of ViewBag.CompletionData


			//get employee 
			var employees = _context.Employee.ToList(); // Retrieve employees from your database

			// Assign employees to ViewBag in the desired format
			ViewBag.Employees = employees.Select(emp => new
			{
				Name = emp.Name,
				Email = emp.Email,
				Availability = emp.Availability
			});


			var ticketsByUser = _context.Tickets
	  .GroupBy(t => t.UserAssigned)
	  .Select(g => new { User = g.Key, Count = g.Count() })
	  .ToList();

			var uniqueMembers = ticketsByUser.Select(entry => entry.User).ToList();
			var memberOccurrences = ticketsByUser.Select(entry => entry.Count).ToList();

			ViewBag.UniqueMembers = uniqueMembers; // Use ViewBag to store unique members
			ViewBag.MemberOccurrences = memberOccurrences; // Use ViewBag to store member occurrences




			return View();

		}




		////GET: Tickets
		//public async Task<IActionResult> Index()
		//{
		//    return View(await _context.Tickets.ToListAsync());

		//}

		//[HttpGet]
		//public IActionResult GetEmployees()
		//{
		//	var employees = _context.Employee.ToList(); // Retrieve employees from your database
		//	return Json(employees); // Return employees as JSON
		//}


		[HttpPost]
        public List<object> GetPointsData()
        {
            List<object> data = new List<object>();

            List<int> xLabels = Enumerable.Range(1, 4).ToList(); // Limit to 4 tickets

            // Retrieve corresponding data for these tickets
            List<int> Point = _context.Tickets
                                .OrderByDescending(p => p.PointPerTicket)
                                .Take(4) // Limiting to 4 tickets
                                .Select(p => p.PointPerTicket).ToList();

            data.Add(xLabels);
            data.Add(Point);

            return data;
        }


		//[HttpPost]
		//public List<object> GetCompletionData()
		//{

		//        List<object> data = new List<object>();

		//        // Get counts of Complete and Incomplete tickets
		//        int completeCount = _context.Tickets.Count(p => p.Status == "Completed");
		//        int incompleteCount = _context.Tickets.Count(p => p.Status == "Incomplete");
		//    int inProgressCount = _context.Tickets.Count(p => p.Status == "In Progress");
		//    // Create labels and corresponding data
		//    List<string> labels = new List<string> { "Complete", "Incomplete", "In Progress" };
		//        List<int> ticketCounts = new List<int> { completeCount, incompleteCount, inProgressCount };

		//        // Add labels and counts to data list
		//        data.Add(labels);
		//        data.Add(ticketCounts);

		//        return data;

		//}

		// This method will be a part of your Controller
		
			[HttpPost]
			public IActionResult GetUserAssigned()
			{
				var ticketsByUser = _context.Tickets
					.GroupBy(t => t.UserAssigned)
					.Select(g => new { User = g.Key, Count = g.Count() })
					.ToList();

				var ticketNames = _context.Tickets
					.Select(t => t.Name)
					.Distinct()
					.ToList();

				List<object> data = new List<object>();

				List<string> users = ticketsByUser.Select(entry => entry.User).ToList();
				List<int> counts = ticketsByUser.Select(entry => entry.Count).ToList();

				data.Add(ticketNames);
				data.Add(counts);

				return Json(new { chartLabels = ticketNames, chartData = counts });
			}

		


		public ActionResult PriorityChartData()
        {
            var priorityData = _context.Tickets
                .GroupBy(t => t.Priority)
                .Select(g => new
                {
                    Priority = g.Key,
                    SprintStoryPointTotal = g.Sum(t => t.SprintStoryPointLimit),
                    PointPerTicketTotal = g.Sum(t => t.PointPerTicket)
                })
                .ToList();

            return Json(priorityData);
        }


        public async Task<IActionResult> ShowTicketSearch()
        {
            return View();
        }

        // POST: ProjectDatas  changeed String to string
        public async Task<IActionResult> ShowTicketSearchResults(string SearchPhrase)
        {
            return View("Index", await _context.Tickets.Where(x => x.Name.Contains
            (SearchPhrase)).ToListAsync());
        }



        // GET: Tickets/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Tickets == null)
            {
                return NotFound();
            }

            var tickets = await _context.Tickets
                .FirstOrDefaultAsync(m => m.ID == id);
            if (tickets == null)
            {
                return NotFound();
            }

            return View(tickets);
        }

        // GET: Tickets/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Tickets/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Name,Description,ProjectName,Company,UserAssigned,AssignedBy,Status,Priority,Category,DueBy,UpdatedDate,StartDate,icon,MaxTicketsPerSprint,SprintStoryPointLimit,PointPerTicket")] Tickets tickets)
        {
            if (ModelState.IsValid)
            {
                _context.Add(tickets);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(tickets);
        }

        // GET: Tickets/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Tickets == null)
            {
                return NotFound();
            }

            var tickets = await _context.Tickets.FindAsync(id);
            if (tickets == null)
            {
                return NotFound();
            }
            return View(tickets);
        }

        // POST: Tickets/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,Description,ProjectName,Company,UserAssigned,AssignedBy,Status,Priority,Category,DueBy,UpdatedDate,StartDate,icon,MaxTicketsPerSprint,SprintStoryPointLimit,PointPerTicket")] Tickets tickets)
        {
            if (id != tickets.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tickets);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketsExists(tickets.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(tickets);
        }

        // GET: Tickets/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Tickets == null)
            {
                return NotFound();
            }

            var tickets = await _context.Tickets
                .FirstOrDefaultAsync(m => m.ID == id);
            if (tickets == null)
            {
                return NotFound();
            }

            return View(tickets);
        }

        // POST: Tickets/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Tickets == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Tickets'  is null.");
            }
            var tickets = await _context.Tickets.FindAsync(id);
            if (tickets != null)
            {
                _context.Tickets.Remove(tickets);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TicketsExists(int id)
        {
          return (_context.Tickets?.Any(e => e.ID == id)).GetValueOrDefault();
        }
    }
}
