# Sensor options (Dev's research + my edits)

Looked at 4 things:

1. **Resistive "fork" type sensors** (the cheap YL-69 style) - ~$1. Everyone online says they corrode in a few weeks. Skip unless nothing else
2. **Capacitive v1.2** - ~$2-3 each on AliExpress. Analog out. Seems to be the standard hobbyist pick. Reports of QC problems tho (missing resistor / wrong timer chip on some batches)
3. **Industrial-ish RS485 probe** (the ones w/ moisture + temp + EC) - $25-40 each. Way better but eats the whole budget with 8 nodes. Maybe 1 as a "reference" probe to calibrate against?
4. **DIY gypsum blocks / watermark sensors** - the ones farmers use. Need an AC excitation circuit. Interesting but too much work for pilot

**leaning:** capacitive x 10 (2 spares), + 1 reference probe if grant comes through

see [[02a_capacitive-vs-resistive]] for notes on why I'm not just going w/ the cheapest one

Open q: how do we calibrate? Raised bed soil is basically potting mix + compost, in-ground plots are clay-ish. Same sensor will read differently. Probably need per-bed calibration curve (dry / saturated reading) -> Dev says this is normal

Also: waterproofing the top of the board. Nail polish? conformal coating? heat shrink + hot glue? Look up.
