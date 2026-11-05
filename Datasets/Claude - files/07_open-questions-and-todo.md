OPEN QUESTIONS / TODO  (updated 2 Oct)

[x] confirm outlet in shed (yes, flaky)
[x] range test
[ ] measure sleep current on modified Pro Mini  <- BLOCKER for power plan
[ ] order sensors (wait for QC thing? or order from 2 sellers and compare)
[ ] decide gateway: single-channel ESP32 vs real LoRaWAN gateway
[ ] where does data go? options: (a) Google Sheet via script (b) Home Assistant on Tomas's Pi (c) small web page. Tomas keeps saying HA. I think (a) is simplest for v1
[ ] who maintains this after the pilot?? nobody has said. Joanna said "you guys?" -> not ok, need a plan or it'll rot
[ ] calibration procedure written down so a non-technical person can do it
[ ] waterproofing test: leave a sealed sensor in a bucket for 2 wks
[ ] grant app (see budget note)
[ ] LED posts - decide if doing this at all. battery cost.
[ ] winter plan - take them in? 

random thoughts
- the sensor reads 0-100 "percent" but that's not real VWC, it's a made up scale. Be honest about that on the page. maybe "dry / ok / wet" is better than a number
- what threshold counts as "needs water"? depends on crop. tomatoes != lettuce. Ask Joanna or someone who knows
- should we log temp too? cheap to add (DS18B20)
- Dev mentioned timestamps drifting on the Pro Mini since no RTC. Probably have the gateway stamp the time on receipt, easier
