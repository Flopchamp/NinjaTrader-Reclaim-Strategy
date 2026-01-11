#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it. 
namespace NinjaTrader.NinjaScript.Indicators
{
	public class ReclaimLevelsIndicator : Indicator
	{
		#region Variables
		// List to hold reclaim levels entered by user
		private List<double> reclaimLevels = new List<double>();
		
		// List to hold major support levels entered by user
		private List<double> majSupportLevels = new List<double>();
		
		// Variables to track trading logic state
		private double currentMajSupportLevel = 0; // The MAJ SUPPORT level we're tracking
		private bool hasCrossedBelowByPoints = false; // Track if condition #1 is met
		private DateTime timerStartTime = DateTime.MinValue; // When timer started
		private bool isTimerRunning = false; // Is the timer currently running
		private bool hasReclaimedLevel = false; // Track if level was reclaimed
		private bool hasEnteredTrade = false; // Track if trade entry signal was shown
		
		// Drawing object tags for management
		private string crossedBelowTag = "";
		private string reclaimedTag = "";
		private string enterTradeTag = "";
		private string timerTag = "";
		
		#endregion
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Reclaim Levels Indicator - Plots support levels and identifies reclaim opportunities";
				Name										= "Reclaim Levels Indicator";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				
				// User input properties with default values
				ReclaimLevel1		= 7057;
				ReclaimLevel2		= 7061;
				ReclaimLevel3		= 7069;
				
				MajSupport1			= 6855;
				MajSupport2			= 6859;
				MajSupport3			= 6866;
				
				LowOfYesterday		= 6835;
				LowOf2DaysAgo		= 6825;
				
				PointsAboveTicks	= 8;  // 8 ticks = 2 points for ES/MES
				HeldAboveTimeSeconds = 270; // 4 minutes 30 seconds (4*60 + 30)
				StopDownTicks		= 40; // 40 ticks = 10 points for ES/MES
			}
			else if (State == State.Configure)
			{
				// Build lists of levels from user inputs
				reclaimLevels.Clear();
				if (ReclaimLevel1 > 0) reclaimLevels.Add(ReclaimLevel1);
				if (ReclaimLevel2 > 0) reclaimLevels.Add(ReclaimLevel2);
				if (ReclaimLevel3 > 0) reclaimLevels.Add(ReclaimLevel3);
				
				majSupportLevels.Clear();
				if (MajSupport1 > 0) majSupportLevels.Add(MajSupport1);
				if (MajSupport2 > 0) majSupportLevels.Add(MajSupport2);
				if (MajSupport3 > 0) majSupportLevels.Add(MajSupport3);
				
				// Sort levels in descending order (highest to lowest)
				reclaimLevels.Sort((a, b) => b.CompareTo(a));
				majSupportLevels.Sort((a, b) => b.CompareTo(a));
			}
			else if (State == State.DataLoaded)
			{
				// Draw all horizontal lines when data is loaded
				DrawAllLevels();
			}
		}

		protected override void OnBarUpdate()
		{
			// Need at least 1 bar to process
			if (CurrentBar < 1)
				return;
			
			// Get current price (Close of current bar)
			double lastPrice = Close[0];
			
			// Main trading logic
			ProcessTradingLogic(lastPrice);
		}
		
		#region Drawing Methods
		
		/// <summary>
		/// Draw all horizontal lines and labels for reclaim levels, major support, and yesterday/2-day lows
		/// </summary>
		private void DrawAllLevels()
		{
			// Draw Reclaim Levels (Gold color)
			for (int i = 0; i < reclaimLevels.Count; i++)
			{
				string lineTag = "ReclaimLine_" + i;
				string textTag = "ReclaimText_" + i;
				
				// Draw horizontal line
				Draw.HorizontalLine(this, lineTag, reclaimLevels[i], Brushes.Gold, DashStyleHelper.Solid, 2);
				
				// Draw text label
				Draw.Text(this, textTag, false, "RECLAIM LEVEL - " + reclaimLevels[i].ToString("F2"), 
					0, reclaimLevels[i], 0, Brushes.White, 
					new SimpleFont("Montserrat", 20) { Bold = false }, 
					TextAlignment.Left, Brushes.Gold, Brushes.Gold, 95);
			}
			
			// Draw Major Support Levels (Medium Sea Green color)
			for (int i = 0; i < majSupportLevels.Count; i++)
			{
				string lineTag = "MajSupportLine_" + i;
				string textTag = "MajSupportText_" + i;
				
				// Draw horizontal line
				Draw.HorizontalLine(this, lineTag, majSupportLevels[i], Brushes.MediumSeaGreen, DashStyleHelper.Solid, 2);
				
				// Draw text label
				Draw.Text(this, textTag, false, "MAJOR SUPPORT LEVEL - " + majSupportLevels[i].ToString("F2"), 
					0, majSupportLevels[i], 0, Brushes.White, 
					new SimpleFont("Montserrat", 20) { Bold = false }, 
					TextAlignment.Left, Brushes.MediumSeaGreen, Brushes.MediumSeaGreen, 95);
			}
			
			// Draw Low of Yesterday (Red color)
			if (LowOfYesterday > 0)
			{
				Draw.HorizontalLine(this, "LowYesterdayLine", LowOfYesterday, Brushes.Red, DashStyleHelper.Solid, 2);
				Draw.Text(this, "LowYesterdayText", false, "LOW YEST - " + LowOfYesterday.ToString("F2"), 
					0, LowOfYesterday, 0, Brushes.White, 
					new SimpleFont("Montserrat", 20) { Bold = false }, 
					TextAlignment.Left, Brushes.Red, Brushes.Red, 95);
			}
			
			// Draw Low of 2 Days Ago (Orange-Red color)
			if (LowOf2DaysAgo > 0)
			{
				Draw.HorizontalLine(this, "Low2DaysAgoLine", LowOf2DaysAgo, Brushes.OrangeRed, DashStyleHelper.Solid, 2);
				Draw.Text(this, "Low2DaysAgoText", false, "LOW 2 DAY - " + LowOf2DaysAgo.ToString("F2"), 
					0, LowOf2DaysAgo, 0, Brushes.White, 
					new SimpleFont("Montserrat", 20) { Bold = false }, 
					TextAlignment.Left, Brushes.OrangeRed, Brushes.OrangeRed, 95);
			}
		}
		
		/// <summary>
		/// Draw "CROSSED 2 pts BELOW" text
		/// </summary>
		private void DrawCrossedBelowText()
		{
			crossedBelowTag = "CrossedBelow_" + CurrentBar;
			Draw.Text(this, crossedBelowTag, false, "CROSSED 2 pts BELOW", 
				0, High[0] + (5 * TickSize), 0, Brushes.White, 
				new SimpleFont("Montserrat", 20) { Bold = false }, 
				TextAlignment.Left, Brushes.SlateGray, Brushes.SlateGray, 95);
		}
		
		/// <summary>
		/// Draw "RECLAIMED LEVEL" text
		/// </summary>
		private void DrawReclaimedLevelText()
		{
			reclaimedTag = "Reclaimed_" + CurrentBar;
			Draw.Text(this, reclaimedTag, false, "RECLAIMED LEVEL", 
				0, High[0] + (5 * TickSize), 0, Brushes.White, 
				new SimpleFont("Montserrat", 20) { Bold = false }, 
				TextAlignment.Left, Brushes.SlateGray, Brushes.SlateGray, 95);
		}
		
		/// <summary>
		/// Draw timer text showing elapsed time
		/// </summary>
		private void DrawTimerText(TimeSpan elapsed)
		{
			if (!string.IsNullOrEmpty(timerTag))
				RemoveDrawObject(timerTag);
				
			timerTag = "Timer_" + CurrentBar;
			string timerText = string.Format("Timer: {0:D2}:{1:D2}:{2:D2}", 
				elapsed.Hours, elapsed.Minutes, elapsed.Seconds);
			
			Draw.Text(this, timerTag, false, timerText, 
				0, High[0] + (10 * TickSize), 0, Brushes.Yellow, 
				new SimpleFont("Montserrat", 16) { Bold = true }, 
				TextAlignment.Left, Brushes.Black, Brushes.Black, 90);
		}
		
		/// <summary>
		/// Draw "ENTER THE TRADE" text
		/// </summary>
		private void DrawEnterTradeText()
		{
			enterTradeTag = "EnterTrade_" + CurrentBar;
			Draw.Text(this, enterTradeTag, false, "ENTER THE TRADE", 
				0, High[0] + (5 * TickSize), 0, Brushes.White, 
				new SimpleFont("Montserrat", 20) { Bold = true }, 
				TextAlignment.Left, Brushes.DarkSlateGray, Brushes.DarkSlateGray, 95);
			
			// Hide timer when trade entry is signaled
			if (!string.IsNullOrEmpty(timerTag))
				RemoveDrawObject(timerTag);
		}
		
		#endregion
		
		#region Trading Logic
		
		/// <summary>
		/// Main trading logic to process conditions and generate signals
		/// </summary>
		private void ProcessTradingLogic(double lastPrice)
		{
			// Convert ticks to price points
			double pointsAbove = PointsAboveTicks * TickSize;
			double stopDown = StopDownTicks * TickSize;
			
			// If we haven't entered a trade yet, check conditions
			if (!hasEnteredTrade)
			{
				// CONDITION #1: Check if price crossed below MAJ SUPPORT by at least 2 points
				if (!hasCrossedBelowByPoints)
				{
					CheckCondition1_CrossedBelow(lastPrice, pointsAbove);
				}
				// CONDITION #2: If condition #1 is met, check for reclaim
				else if (hasCrossedBelowByPoints && currentMajSupportLevel > 0)
				{
					CheckCondition2_Reclaim(lastPrice, pointsAbove);
				}
			}
		}
		
		/// <summary>
		/// Condition #1: Check if price crossed below any MAJ SUPPORT by the required points
		/// </summary>
		private void CheckCondition1_CrossedBelow(double lastPrice, double pointsAbove)
		{
			// Loop through MAJ SUPPORT levels from highest to lowest
			for (int i = 0; i < majSupportLevels.Count; i++)
			{
				double majSupport = majSupportLevels[i];
				
				// Check if last price is below this level by at least the required points
				if (lastPrice <= (majSupport - pointsAbove))
				{
					// Condition #1 is met!
					hasCrossedBelowByPoints = true;
					currentMajSupportLevel = majSupport;
					
					// Draw "CROSSED 2 pts BELOW" text
					DrawCrossedBelowText();
					
					Print(Time[0] + " - Condition #1 Met: Crossed " + pointsAbove/TickSize + " ticks below MAJ SUPPORT " + majSupport);
					
					break; // Only track one level at a time
				}
			}
		}
		
		/// <summary>
		/// Condition #2: Check if price reclaimed the MAJ SUPPORT level and held above for required time
		/// </summary>
		private void CheckCondition2_Reclaim(double lastPrice, double pointsAbove)
		{
			// Check if price crossed back above the MAJ SUPPORT level
			if (lastPrice >= currentMajSupportLevel)
			{
				// Price is above the level
				if (!isTimerRunning)
				{
					// Start the timer
					isTimerRunning = true;
					timerStartTime = Time[0];
					hasReclaimedLevel = true;
					
					// Update text to show "RECLAIMED LEVEL"
					DrawReclaimedLevelText();
					
					Print(Time[0] + " - Timer Started: Price reclaimed MAJ SUPPORT " + currentMajSupportLevel);
				}
				else
				{
					// Timer is running, check elapsed time
					TimeSpan elapsed = Time[0] - timerStartTime;
					
					// Show timer on chart
					DrawTimerText(elapsed);
					
					// Check if held above for required time
					if (elapsed.TotalSeconds >= HeldAboveTimeSeconds)
					{
						// CONDITION #2 MET! Enter the trade
						hasEnteredTrade = true;
						isTimerRunning = false;
						
						// Draw "ENTER THE TRADE" text
						DrawEnterTradeText();
						
						Print(Time[0] + " - ENTER THE TRADE! Held above " + currentMajSupportLevel + " for " + HeldAboveTimeSeconds + " seconds");
					}
				}
			}
			else if (lastPrice < currentMajSupportLevel && isTimerRunning)
			{
				// Price dropped back below the level while timer was running
				// Stop and reset timer
				isTimerRunning = false;
				timerStartTime = DateTime.MinValue;
				
				// Hide timer
				if (!string.IsNullOrEmpty(timerTag))
					RemoveDrawObject(timerTag);
				
				Print(Time[0] + " - Timer Reset: Price dropped below MAJ SUPPORT " + currentMajSupportLevel);
				
				// Check if price moved down to next lower MAJ SUPPORT
				int currentLevelIndex = majSupportLevels.IndexOf(currentMajSupportLevel);
				if (currentLevelIndex < majSupportLevels.Count - 1)
				{
					double nextLowerLevel = majSupportLevels[currentLevelIndex + 1];
					
					if (lastPrice <= nextLowerLevel)
					{
						// Reset and go back to Condition #1 with new level
						ResetTradingLogic();
						Print(Time[0] + " - Reset to Condition #1: Price reached next lower MAJ SUPPORT");
					}
				}
			}
		}
		
		/// <summary>
		/// Reset all trading logic variables to start fresh
		/// </summary>
		private void ResetTradingLogic()
		{
			hasCrossedBelowByPoints = false;
			currentMajSupportLevel = 0;
			isTimerRunning = false;
			timerStartTime = DateTime.MinValue;
			hasReclaimedLevel = false;
			hasEnteredTrade = false;
			
			// Clear timer display
			if (!string.IsNullOrEmpty(timerTag))
				RemoveDrawObject(timerTag);
		}
		
		#endregion

		#region Properties
		
		// Reclaim Levels
		[NinjaScriptProperty]
		[Display(Name="Reclaim Level 1", Description="First reclaim level price", Order=1, GroupName="1. Reclaim Levels")]
		public double ReclaimLevel1
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Reclaim Level 2", Description="Second reclaim level price", Order=2, GroupName="1. Reclaim Levels")]
		public double ReclaimLevel2
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Reclaim Level 3", Description="Third reclaim level price", Order=3, GroupName="1. Reclaim Levels")]
		public double ReclaimLevel3
		{ get; set; }
		
		// Major Support Levels
		[NinjaScriptProperty]
		[Display(Name="Major Support 1", Description="First major support level price", Order=1, GroupName="2. Major Support Levels")]
		public double MajSupport1
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Major Support 2", Description="Second major support level price", Order=2, GroupName="2. Major Support Levels")]
		public double MajSupport2
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Major Support 3", Description="Third major support level price", Order=3, GroupName="2. Major Support Levels")]
		public double MajSupport3
		{ get; set; }
		
		// Yesterday and 2 Days Ago Lows
		[NinjaScriptProperty]
		[Display(Name="Low of Yesterday", Description="Yesterday's low price", Order=1, GroupName="3. Historical Lows")]
		public double LowOfYesterday
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Low of 2 Days Ago", Description="Low from 2 days ago", Order=2, GroupName="3. Historical Lows")]
		public double LowOf2DaysAgo
		{ get; set; }
		
		// Trading Logic Parameters
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Points Above (Ticks)", Description="Number of ticks above level (8 ticks = 2 points)", Order=1, GroupName="4. Trading Parameters")]
		public int PointsAboveTicks
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Held Above Time (Seconds)", Description="Time in seconds price must hold above level (270 = 4m 30s)", Order=2, GroupName="4. Trading Parameters")]
		public int HeldAboveTimeSeconds
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Stop Down (Ticks)", Description="Number of ticks for stop loss (40 ticks = 10 points)", Order=3, GroupName="4. Trading Parameters")]
		public int StopDownTicks
		{ get; set; }
		
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private ReclaimLevelsIndicator[] cacheReclaimLevelsIndicator;
		public ReclaimLevelsIndicator ReclaimLevelsIndicator(double reclaimLevel1, double reclaimLevel2, double reclaimLevel3, double majSupport1, double majSupport2, double majSupport3, double lowOfYesterday, double lowOf2DaysAgo, int pointsAboveTicks, int heldAboveTimeSeconds, int stopDownTicks)
		{
			return ReclaimLevelsIndicator(Input, reclaimLevel1, reclaimLevel2, reclaimLevel3, majSupport1, majSupport2, majSupport3, lowOfYesterday, lowOf2DaysAgo, pointsAboveTicks, heldAboveTimeSeconds, stopDownTicks);
		}

		public ReclaimLevelsIndicator ReclaimLevelsIndicator(ISeries<double> input, double reclaimLevel1, double reclaimLevel2, double reclaimLevel3, double majSupport1, double majSupport2, double majSupport3, double lowOfYesterday, double lowOf2DaysAgo, int pointsAboveTicks, int heldAboveTimeSeconds, int stopDownTicks)
		{
			if (cacheReclaimLevelsIndicator != null)
				for (int idx = 0; idx < cacheReclaimLevelsIndicator.Length; idx++)
					if (cacheReclaimLevelsIndicator[idx] != null && cacheReclaimLevelsIndicator[idx].ReclaimLevel1 == reclaimLevel1 && cacheReclaimLevelsIndicator[idx].ReclaimLevel2 == reclaimLevel2 && cacheReclaimLevelsIndicator[idx].ReclaimLevel3 == reclaimLevel3 && cacheReclaimLevelsIndicator[idx].MajSupport1 == majSupport1 && cacheReclaimLevelsIndicator[idx].MajSupport2 == majSupport2 && cacheReclaimLevelsIndicator[idx].MajSupport3 == majSupport3 && cacheReclaimLevelsIndicator[idx].LowOfYesterday == lowOfYesterday && cacheReclaimLevelsIndicator[idx].LowOf2DaysAgo == lowOf2DaysAgo && cacheReclaimLevelsIndicator[idx].PointsAboveTicks == pointsAboveTicks && cacheReclaimLevelsIndicator[idx].HeldAboveTimeSeconds == heldAboveTimeSeconds && cacheReclaimLevelsIndicator[idx].StopDownTicks == stopDownTicks && cacheReclaimLevelsIndicator[idx].EqualsInput(input))
						return cacheReclaimLevelsIndicator[idx];
			return CacheIndicator<ReclaimLevelsIndicator>(new ReclaimLevelsIndicator(){ ReclaimLevel1 = reclaimLevel1, ReclaimLevel2 = reclaimLevel2, ReclaimLevel3 = reclaimLevel3, MajSupport1 = majSupport1, MajSupport2 = majSupport2, MajSupport3 = majSupport3, LowOfYesterday = lowOfYesterday, LowOf2DaysAgo = lowOf2DaysAgo, PointsAboveTicks = pointsAboveTicks, HeldAboveTimeSeconds = heldAboveTimeSeconds, StopDownTicks = stopDownTicks }, input, ref cacheReclaimLevelsIndicator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.ReclaimLevelsIndicator ReclaimLevelsIndicator(double reclaimLevel1, double reclaimLevel2, double reclaimLevel3, double majSupport1, double majSupport2, double majSupport3, double lowOfYesterday, double lowOf2DaysAgo, int pointsAboveTicks, int heldAboveTimeSeconds, int stopDownTicks)
		{
			return indicator.ReclaimLevelsIndicator(Input, reclaimLevel1, reclaimLevel2, reclaimLevel3, majSupport1, majSupport2, majSupport3, lowOfYesterday, lowOf2DaysAgo, pointsAboveTicks, heldAboveTimeSeconds, stopDownTicks);
		}

		public Indicators.ReclaimLevelsIndicator ReclaimLevelsIndicator(ISeries<double> input , double reclaimLevel1, double reclaimLevel2, double reclaimLevel3, double majSupport1, double majSupport2, double majSupport3, double lowOfYesterday, double lowOf2DaysAgo, int pointsAboveTicks, int heldAboveTimeSeconds, int stopDownTicks)
		{
			return indicator.ReclaimLevelsIndicator(input, reclaimLevel1, reclaimLevel2, reclaimLevel3, majSupport1, majSupport2, majSupport3, lowOfYesterday, lowOf2DaysAgo, pointsAboveTicks, heldAboveTimeSeconds, stopDownTicks);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.ReclaimLevelsIndicator ReclaimLevelsIndicator(double reclaimLevel1, double reclaimLevel2, double reclaimLevel3, double majSupport1, double majSupport2, double majSupport3, double lowOfYesterday, double lowOf2DaysAgo, int pointsAboveTicks, int heldAboveTimeSeconds, int stopDownTicks)
		{
			return indicator.ReclaimLevelsIndicator(Input, reclaimLevel1, reclaimLevel2, reclaimLevel3, majSupport1, majSupport2, majSupport3, lowOfYesterday, lowOf2DaysAgo, pointsAboveTicks, heldAboveTimeSeconds, stopDownTicks);
		}

		public Indicators.ReclaimLevelsIndicator ReclaimLevelsIndicator(ISeries<double> input , double reclaimLevel1, double reclaimLevel2, double reclaimLevel3, double majSupport1, double majSupport2, double majSupport3, double lowOfYesterday, double lowOf2DaysAgo, int pointsAboveTicks, int heldAboveTimeSeconds, int stopDownTicks)
		{
			return indicator.ReclaimLevelsIndicator(input, reclaimLevel1, reclaimLevel2, reclaimLevel3, majSupport1, majSupport2, majSupport3, lowOfYesterday, lowOf2DaysAgo, pointsAboveTicks, heldAboveTimeSeconds, stopDownTicks);
		}
	}
}

#endregion
