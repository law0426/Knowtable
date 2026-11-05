# LoRa range test - Sat 21 Sept

Dev + me, ~2 hrs. Borrowed 2 RFM95 (915 MHz) modules on Pro Minis. One as "gateway" (taped to shed roof, ~2.5m up), one walked around.

Settings: SF7, BW125, 17 dBm, simple ping every 5 sec, log RSSI/SNR on the gateway.

| spot | approx dist | RSSI | notes |
|---|---|---|---|
| bed 1 (near shed) | 15 m | -68 | fine |
| bed 6 | 45 m | -84 | fine |
| in-ground plot A | 90 m | -92 | fine |
| in-ground plot B (far corner) | 140 m | -103 | dropped ~1 in 5 packets |
| behind greenhouse | 70 m | -112 | lost almost all @ SF7 |
| behind greenhouse, SF9 | 70 m | -108 | ok! ~all got through |

takeaways
- plenty of range for the garden itself. The greenhouse is the only problem (metal frame?)
- SF9 fixes it but airtime is ~ 4x longer -> more battery. Only matters for the 1-2 nodes back there. Could set SF per node
- antenna matters: the wire "quarter wave" (8.2 cm) vs. the little spring thing. Wire was ~6 dB better, didn't expect that much. Use wire, keep it vertical

Dev wants a proper gateway = RPi + RAK/Dragino concentrator. Overkill?? A single-channel gateway (ESP32 + RFM95 -> wifi at clubhouse) is probably enough for 8 nodes. But then the gateway needs to be somewhere with wifi - clubhouse is ~ 60 m from shed. Hmm. Could just put gateway IN the clubhouse window. test that!

legal: 915 MHz fine for us, duty cycle isn't an issue at 15 min intervals.
