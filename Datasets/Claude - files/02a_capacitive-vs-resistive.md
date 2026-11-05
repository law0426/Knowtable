# capacitive vs resistive - quick notes

(sub-note of sensor options)

resistive: measures conductivity between 2 prongs. current flows -> electrolysis -> prongs eat themselves. also readings depend on fertilizer salts not just water
  - pro: dirt cheap, simple
  - con: dies. also drift

capacitive: measures dielectric of soil (water has v high permittivity vs air/soil). no exposed metal contact if the coating is intact
  - pro: lasts longer, less salt-sensitive
  - con: edges of the PCB can wick water in if not sealed -> then it degrades anyway
  - con: output is voltage that DROPS when wetter (counterintuitive, easy to flip in code)

typical raw ADC readings people post (3.3V, 12bit):
  air ~ 2800-3000
  water ~ 1200-1400
  so usable range is narrow-ish. 

TODO: test 3 sensors in same cup of soil, see how much unit-to-unit variance. If > 10% need to calibrate each one individually, not just per bed

source stuff I read: some forum thread, a blog by someone w/ 2 yrs of data (can't find link again!! check browser history), the datasheet-that-isn't-really-a-datasheet

conclusion: capacitive, seal the edges w/ epoxy or clear nail polish, accept replacing ~yearly
