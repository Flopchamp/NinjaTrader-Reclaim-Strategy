# NinjaTrader Reclaim Strategy

A NinjaTrader 8 indicator that identifies and tracks reclaim opportunities based on support level analysis and price action. This indicator helps traders identify potential long entry points when price reclaims key levels after meeting specific conditions.

## 📊 Overview

The Reclaim Levels Indicator monitors price action relative to major support levels and reclaim levels, generating entry signals when specific trading conditions are met. It provides visual feedback through horizontal level lines, real-time status messages, and trade entry signals.

## ✨ Features

- **Multiple Level Tracking**: Monitor up to 3 reclaim levels and 3 major support levels simultaneously
- **Visual Level Indicators**: Color-coded horizontal lines for easy chart reading
  - Gold lines for reclaim levels
  - Medium Sea Green for major support levels
  - Red for yesterday's low and 2-day low
- **Automated Trade Logic**: Tracks complex multi-condition entry criteria
- **Real-time Status Updates**: Visual text notifications showing current trading state
- **Customizable Parameters**: Adjustable price levels, time requirements, and tick thresholds

## 🎯 Trading Logic

The indicator identifies trade opportunities based on the following conditions:

1. **Initial Breakdown**: Price crosses below a major support level by a specified number of ticks
2. **Time Requirement**: Price must remain above the level (after reclaim) for a specified duration
3. **Reclaim Confirmation**: Price crosses back above the major support level
4. **Stop Loss Calculation**: Automatically determines stop loss based on configurable tick distance

## 📋 Requirements

- NinjaTrader 8 (Platform version 8.0 or higher)
- Basic knowledge of NinjaScript and NinjaTrader indicators
- Windows operating system

## 🚀 Installation

### Quick Installation

1. **Download the indicator file**
   - Download `ReclaimLevelsIndicator.cs` from this repository

2. **Open NinjaScript Editor**
   - Open NinjaTrader 8
   - Click `Tools` → `Edit NinjaScript`

3. **Import the indicator**
   - In the NinjaScript Editor, expand the `Indicators` folder
   - Right-click on `Indicators` → `New` → `Blank Indicator...`
   - Name it: `ReclaimLevelsIndicator`
   - Replace all default code with the contents of `ReclaimLevelsIndicator.cs`

4. **Compile the indicator**
   - Press `F5` or click `Compile` in the toolbar
   - Ensure there are no compilation errors

5. **Add to your chart**
   - Right-click on your chart → `Indicators`
   - Search for "Reclaim Levels Indicator"
   - Configure your levels and parameters
   - Click `OK`

### Detailed Installation

For step-by-step installation instructions with screenshots and troubleshooting tips, see [Installation_Instructions.txt](Installation_Instructions.txt).

## ⚙️ Configuration

### Input Parameters

| Parameter | Default | Description |
|-----------|---------|-------------|
| **Reclaim Level 1-3** | 7057, 7061, 7069 | Price levels where reclaim opportunities are monitored |
| **MAJ Support 1-3** | 6855, 6859, 6866 | Major support levels that trigger the trading logic |
| **Low of Yesterday** | 6835 | Previous day's low price |
| **Low of 2 Days Ago** | 6825 | Low price from two days ago |
| **Points Above (Ticks)** | 8 | Number of ticks price must cross below support (8 ticks = 2 points for ES/MES) |
| **Held Above Time (Seconds)** | 270 | Time in seconds price must stay above level (270s = 4min 30sec) |
| **Stop Down (Ticks)** | 40 | Distance in ticks for stop loss below level (40 ticks = 10 points for ES/MES) |

### Example Configuration for ES/MES Trading

```
Reclaim Levels: 7057, 7061, 7069
Major Support: 6855, 6859, 6866
Points Above: 8 ticks (2 points)
Held Above Time: 270 seconds (4.5 minutes)
Stop Loss: 40 ticks (10 points)
```

## 📖 Usage

1. **Set Your Levels**: Configure the reclaim and major support levels based on your analysis
2. **Monitor the Chart**: The indicator will display all levels as horizontal lines
3. **Watch for Signals**: Status messages appear in the upper-left corner showing current state:
   - "Crossed Below [level]"
   - "Reclaimed [level] - Timer Started"
   - "**ENTER TRADE**" with stop loss information
4. **Execute Trades**: When the entry signal appears, execute your trade according to the displayed parameters

## 📸 Visual Elements

- **Gold Lines**: Reclaim levels with labels
- **Green Lines**: Major support levels with labels
- **Red Lines**: Yesterday's low and 2-day low
- **Status Messages**: Real-time trading logic state (upper-left corner)
- **Entry Signals**: Bold "**ENTER TRADE**" message with stop loss price

## 🛠️ Customization

You can modify the indicator by editing the source code to:
- Add more price levels (beyond 3 reclaim and 3 support levels)
- Change colors and line styles
- Adjust text label positions and fonts
- Modify the trading logic conditions
- Add alerts or automated order placement

## ⚠️ Disclaimer

This indicator is provided for educational and informational purposes only. Trading futures and derivatives involves substantial risk of loss and is not suitable for all investors. Past performance is not indicative of future results. Always use proper risk management and never risk more than you can afford to lose.

## 📝 License

This project is open source and available for personal and commercial use. Please review the license file for more details.

## 🤝 Contributing

Contributions, issues, and feature requests are welcome! Feel free to check the issues page or submit a pull request.

## 📧 Support

For questions or support:
- Open an issue in this repository
- Review the [Installation_Instructions.txt](Installation_Instructions.txt) for detailed setup help

## 🔄 Version History

- **v1.0.0** - Initial release
  - Basic reclaim level tracking
  - Multi-condition entry logic
  - Visual level indicators
  - Real-time status updates

---

**Made for NinjaTrader 8** | Built with NinjaScript
