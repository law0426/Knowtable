# Power budget

Goal: node runs all season w/o anyone touching it. Joanna's volunteers will NOT remember to recharge things.

**node = MCU + LoRa radio + sensor**

Rough numbers (from datasheets, to be measured):
- MCU sleeping (Pro Mini 3.3V w/ LED + regulator removed): ~5 uA best case. Stock board is more like 3-5 mA because of regulator + LED lol. Have to desolder
- LoRa TX @ 17dBm: ~ 90-120 mA for ~ 100 ms-ish at SF7. longer at higher SF
- sensor: ~ 5 mA, only power it when sampling (switch via GPIO)

reading every 15 min:
- wake + read + TX = maybe 1 sec total @ ~40 mA avg ?? -> 11 uAh per cycle ... *96 cycles/day ~ 1 mAh/day*
- plus sleep ~ 0.005mA * 24h = 0.12 mAh
- so ~ 1.1-1.3 mAh/day  (I'll recheck this, bit hand-wavy)

18650 cell, ~2500 mAh -> 2000 days?? That seems too good. Realistically self-discharge + cold + batt cutoff voltage -> say 1/3 of that. Still > 1 yr. 

=> do we even need solar? Dev thinks maybe not. Marit asked "what if it's cloudy for a week" - not relevant if no solar lol
=> Keep a tiny panel as an option, see [[03a_solar-panel-sizing-scratch]]

ONLY if the sleep current is actually low. If we mess up the desolder and it's 3 mA -> 72 mAh/day -> dead in a month.

TODO measure sleep current w/ multimeter in uA mode (borrow Tomas's? he has the good one)
