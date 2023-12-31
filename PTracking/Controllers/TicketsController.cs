﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PTracking.Data;
using PTracking.Models;
using PTracking.Services;
using PTracking.ViewModel;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static NuGet.Packaging.PackagingConstants;
using PagedList.Mvc;
using PagedList;

namespace PTracking.Controllers
{
    public class TicketsController : Controller
    {
        private readonly ApplicationDbContext _context;
		private readonly ITicketService _ticketService;
		private readonly IEmployeeService _employeeService;
		private readonly IProjectService _projectService;


		public TicketsController(ApplicationDbContext context, IEmployeeService employeeService, ITicketService ticketService, IProjectService projectService)
        {
            _context = context;
			_ticketService = ticketService;
			_employeeService = employeeService;
			_projectService = projectService;
		}

		public async Task<IActionResult> Index()
		{
			// Retrieve tickets data
			var tickets = await _context.Tickets.ToListAsync();

			// Retrieve other necessary data
			int activeTicketCount = _context.Tickets.Count(ticket => ticket.Status == "Incomplete");
            int priorityTickets = _context.Tickets.Count(ticket => ticket.Priority == "High");
            int perSprint = _context.Tickets.Count();

			// Store data in ViewBag
			ViewBag.Tickets = tickets;
			ViewBag.ActiveTicketCount = activeTicketCount;
            ViewBag.PriorityTickets = priorityTickets;
            ViewBag.PerSprint = perSprint;

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
			ViewBag.ChartData = completionData.TicketCounts; // Use ViewBag.ChartData instead of ViewBag.CompletionDa


			var ticketsByUser = _context.Tickets
	  .GroupBy(t => t.UserAssigned)
	  .Select(g => new { User = g.Key, Count = g.Count() })
	  .ToList();

			var uniqueMembers = ticketsByUser.Select(entry => entry.User).ToList();
			var memberOccurrences = ticketsByUser.Select(entry => entry.Count).ToList();

			ViewBag.UniqueMembers = uniqueMembers; // Use ViewBag to store unique members
			ViewBag.MemberOccurrences = memberOccurrences; // Use ViewBag to store member occurrences

			var employees = await GetEmployeeViewModelsAsync();
			ViewBag.Employees = employees;


			return View();

		}

        public async Task<IActionResult> DisplayPriority()
        {
            var displayPriorityTickets = await _ticketService.GetTicketsByPriorityAsync();

            return View(displayPriorityTickets);
        }

		public async Task<IActionResult> DisplayActiveTickets()
		{
			var incompleteOrInProgressTickets = await _ticketService.GetTicketsByStatusAsync();

				return View(incompleteOrInProgressTickets); 
			
			
		}
        public async Task<IActionResult> DisplayTicketsBySprint()
        {
            var quarter = await _ticketService.GetSprintQuarterAsync();
            return View(quarter);
        }

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

        public IActionResult ShowTicketSearch()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ChangeUserStatus(int employeeId, string newStatus)
        {
            var employee = _context.Employee.Find(employeeId);

            if (employee != null)
            {
                employee.Availability = newStatus;

                _context.SaveChanges(); // Save changes to the database

                return Ok("Status updated successfully");
            }
            else
            {
                return NotFound("Employee not found");
            }
        }


        //public async Task<IActionResult> ShowTicketSearch()
        //{
        //    return View();
        //}

        // POST: ProjectDatas  changeed String to string
        public async Task<IActionResult> ShowTicketSearchResults(string SearchPhrase)
        {
            return View("Index", await _context.Tickets.Where(x => x.Name.Contains
            (SearchPhrase)).ToListAsync());
        }


		public async Task<IActionResult> PopulateEmployeeData()
		{
			var employees = await GetEmployeeViewModelsAsync();
			return View(employees);
		}


		public async Task<List<EmployeeViewModel>> GetEmployeeViewModelsAsync()
		{
			var employees = await _employeeService.GetAllEmployeesAsync();

			var employeeViewModels = employees.Select(employee => new EmployeeViewModel
			{
				icon = employee.icon,
				Name = employee.Name,
				Email = employee.Email,
				Availability = employee.Availability

				// Map other properties as needed
			}).ToList();

			return employeeViewModels;
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
        public async Task<IActionResult> Create([Bind("ID,Name,Description,ProjectName,Company,UserAssigned,AssignedBy,Status,Priority,Category,DueBy,UpdatedDate,StartDate,icon,MaxTicketsPerSprint,SprintStoryPointLimit,PointPerTicket,Quarter")] Tickets tickets)
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
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,Description,ProjectName,Company,UserAssigned,AssignedBy,Status,Priority,Category,DueBy,UpdatedDate,StartDate,icon,MaxTicketsPerSprint,SprintStoryPointLimit,PointPerTicket,Quarter")] Tickets tickets)
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
