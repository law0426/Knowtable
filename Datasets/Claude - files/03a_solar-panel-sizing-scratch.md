scratch - solar sizing

0.5W panel, 5V, ~100mA peak. 
at ~45N so ~ 3-4 peak sun hrs summer, ~1 in winter
summer: 100mA * 3.5h = 350 mAh/day  (way more than needed, <1.5 mAh)
winter: 100 * 1 = 100 mAh, still plenty... but panels covered in snow/dirt/leaves etc, say 10% -> 10 mAh. STILL more than the ~1.3 mAh load

so even a tiny 0.1W panel would probably be fine?? 
-> charge controller is the real cost/complexity, not the panel. TP4056 module ~ $0.50 but has no low-batt protection unless the one with DW01 chip. and it keeps the LED on = drain (remove it)

shade! beds 4 and 5 are under the apple tree - check how much sun they get. If < 2 hrs, forget solar there and just use bigger battery

decision: skip solar for v1. Revisit if batteries die earlier than expected.
(Dev will probably disagree, he likes the solar idea)
