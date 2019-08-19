using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using tourneybracket.Data;
using tourneybracket.Models;

namespace tourneybracket.Controllers
{
    public class MatchesController : Controller
    {
        private readonly BracketContext _context;

        public MatchesController(BracketContext context)
        {
            _context = context;
        }

        // GET: Matches
        public async Task<IActionResult> Index()
        {
            var bracketContext = _context.Matches.Include(m => m.Bracket).Include(m => m.TeamA).Include(m => m.TeamB);
            return View(await bracketContext.ToListAsync());
        }

        // GET: Matches/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var match = await _context.Matches
                .Include(m => m.Bracket)
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .FirstOrDefaultAsync(m => m.MatchID == id);
            if (match == null)
            {
                return NotFound();
            }

            return View(match);
        }

        // GET: Matches/Create
        public IActionResult Create()
        {
            ViewData["BracketID"] = new SelectList(_context.Brackets, "id", "BracketName");
            ViewData["TeamAID"] = new SelectList(_context.Teams, "id", "TeamName");
            ViewData["TeamBID"] = new SelectList(_context.Teams, "id", "TeamName");
            return View();
        }

        // POST: Matches/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MatchID,MatchNumber,TeamAID,TeamBID,TeamAScore,TeamBScore,WinnerID,BracketID,RoundNumber")] Match match)
        {
            if (ModelState.IsValid)
            {
                _context.Add(match);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["BracketID"] = new SelectList(_context.Brackets, "id", "BracketName", match.BracketID);
            ViewData["TeamAID"] = new SelectList(_context.Teams, "id", "TeamName", match.TeamAID);
            ViewData["TeamBID"] = new SelectList(_context.Teams, "id", "TeamName", match.TeamBID);
            return View(match);
        }

        // GET: Matches/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var match = await _context.Matches.FindAsync(id);
            if (match == null)
            {
                return NotFound();
            }
            ViewData["BracketID"] = new SelectList(_context.Brackets, "id", "BracketName", match.BracketID);
            ViewData["TeamAID"] = new SelectList(_context.Teams, "id", "TeamName", match.TeamAID);
            ViewData["TeamBID"] = new SelectList(_context.Teams, "id", "TeamName", match.TeamBID);
            return View(match);
        }

        // POST: Matches/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MatchID,MatchNumber,TeamAID,TeamBID,TeamAScore,TeamBScore,WinnerID,BracketID,RoundNumber")] Match match)
        {
            if (id != match.MatchID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (match.TeamAScore > match.TeamBScore)
                    {
                        match.WinnerID = match.TeamAID;
                    }
                    else if (match.TeamAScore < match.TeamBScore)
                    {
                        match.WinnerID = match.TeamBID;
                    }
                    _context.Update(match);
                    await _context.SaveChangesAsync();
                    //string query = "SELECT COUNT([MatchNumber]) FROM [dbo].[Match] Where [RoundNumber] = {0} and [WinnerID] is NOT NULL";
                    var matchesRemaining = _context.Matches
                        .Where(m => m.RoundNumber == match.RoundNumber && m.WinnerID == null)

                        .Count();
                    //select count(*) from Match where RoundNumber = 1 and WinnerID = Null
                    
                    if (matchesRemaining == 0)
                    {
                        List<int> winners = new List<int>();
                        var bID = _context.Matches
                            .Select(m => m.BracketID)
                            .First();
                        var matchNumb = _context.Matches
                            .Select(m => m.MatchNumber)
                            .Last();
                        var RoundNumb = _context.Matches
                            .Select(m => m.RoundNumber)
                            .Last();
                        RoundNumb = RoundNumb + 1;
                        matchNumb = matchNumb + 1;
                        var winnersCircle = _context.Matches.Select(m => new
                        {
                            BracketID = m.BracketID,
                            WinnerID = m.WinnerID
                        })
                        .Where(m => m.BracketID == bID);
                        
                        foreach (var item in winnersCircle)
                        {
                            winners.Add(Convert.ToInt32(item.WinnerID));
                        }
                        if (winners.Count() > 2)
                        {
                            List<Match> matchList = new List<Match>();
                            winners.Sort();
                            int WinnerLen = winners.Count();
                            List<int> topHalf = winners.Take(WinnerLen/2).ToList();
                            List<int> bottomHalf = winners.Skip(WinnerLen/2).ToList();
                            foreach (var item in topHalf)
                            {
                                Debug.WriteLine("Top Half" + item);
                            }
                            foreach (var item in bottomHalf)
                            {
                                Debug.WriteLine("Bottom Half" + item);
                            }

                            for (int x = 0; x < topHalf.Count(); x++)
                            {
                                if (topHalf[x] < bottomHalf[x])
                                {
                                    var matchInitial = new Match()
                                    {
                                        BracketID = bID,
                                        TeamAID = topHalf[x],
                                        TeamBID = bottomHalf[x],
                                        RoundNumber = RoundNumb,
                                        MatchNumber = matchNumb + x,
                                    };
                                    matchList.Add(matchInitial);
                                    _context.Matches.Add(matchInitial);
                                    _context.SaveChanges();
                                }
                                else
                                {
                                    var matchInitial = new Match()
                                    {
                                        BracketID = bID,
                                        TeamBID = topHalf[x],
                                        TeamAID = bottomHalf[x],
                                        RoundNumber = RoundNumb,
                                        MatchNumber = matchNumb + x,
                                    };
                                    matchList.Add(matchInitial);
                                    _context.Matches.Add(matchInitial);
                                    _context.SaveChanges();
                                }
                            }
                        }
                    }

                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MatchExists(match.MatchID))
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
            ViewData["BracketID"] = new SelectList(_context.Brackets, "id", "BracketName", match.BracketID);
            ViewData["TeamAID"] = new SelectList(_context.Teams, "id", "TeamName", match.TeamAID);
            ViewData["TeamBID"] = new SelectList(_context.Teams, "id", "TeamName", match.TeamBID);
            return View(match);
        }

        // GET: Matches/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var match = await _context.Matches
                .Include(m => m.Bracket)
                .Include(m => m.TeamA)
                .Include(m => m.TeamB)
                .FirstOrDefaultAsync(m => m.MatchID == id);
            if (match == null)
            {
                return NotFound();
            }

            return View(match);
        }

        // POST: Matches/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var match = await _context.Matches.FindAsync(id);
            _context.Matches.Remove(match);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MatchExists(int id)
        {
            return _context.Matches.Any(e => e.MatchID == id);
        }
    }
}
