# Spike: voice audition (English and Romanian)

Question: which tool and voices give the warm, friendly female guide voice ("Eva") for English now and Romanian later.

## Verdict

PASS with a caveat. The user listened to Google Chirp 3 HD only (they chose not to audition ElevenLabs or Azure) and said "Leda sounds good". Leda is chosen as Eva's voice for English. Romanian with Leda is the same voice family and is expected to work, but the user's remark did not separately confirm Romanian pronunciation and diacritics, and they gave no per-language ranking. So Romanian pronunciation and the diacritics `ă â î ș ț` are confirmed at the first Romanian voice generation (M4), not here.

This is a single user-reported observation (2026-09-21). The other two vendors were not heard; their entries below stay as researched terms only.

## Evidence

### Terms (checked 2026-09-21, from the vendors' own pages unless marked)

| | ElevenLabs | Microsoft Azure AI Speech | Google Cloud Text-to-Speech |
|---|---|---|---|
| Free tier | Free plan: 10k credits per month (about 1 credit per character for TTS, so roughly 10k characters) | F0 tier: 0.5 million neural characters per month (official pricing page) | Chirp 3 HD: first 1 million characters per month free (official pricing page) |
| Cheapest paid plan | Starter, $6 per month, 30k credits per month | Pay as you go, no monthly plan. Official page renders prices as "$-"; third-party sources state about $16 per 1M characters (standard neural) and $22 per 1M (Neural HD). Not confirmed from a vendor page | Pay as you go, $30 per 1M characters for Chirp 3 HD (official pricing page); no monthly plan |
| Commercial use of output in a shipped app on the free tier | No. Free users may use the service "only for non-commercial purposes" (Terms of Service). Commercial license starts at Starter | Not stated as restricted on F0 in the pages read; Code of Conduct governs use. Not confirmed whether F0 has any extra commercial limit | Not stated as restricted in the pages read. Not confirmed from a dedicated TTS commercial-use statement |
| Attribution | Free plan: public content must credit "elevenlabs.io" or "11.ai" in the title. Paid plans: none stated | None found. Code of Conduct requires disclosing that generated voices are synthetic so users are not deceived into thinking they talk to a real person | None found in the pages read |
| Child-directed / COPPA restriction | None stated in the Terms of Service (only 18+ for account holders) | None specific to children found. Code of Conduct bans exploiting age vulnerabilities to cause harm and requires the synthetic-voice disclosure | None found in Service Specific Terms (Google Cloud). Not confirmed elsewhere |
| ro-RO voices | Romanian supported by Eleven v3 (74 languages), Multilingual v2 and Flash v2.5; specific Romanian voice names not confirmed (voice library is browsed in the web studio) | Only 2: `ro-RO-AlinaNeural` (female), `ro-RO-IoanNeural` (male); no HD Romanian voice | Chirp 3 HD female: `ro-RO-Chirp3-HD-Aoede`, `-Kore`, `-Leda`, `-Zephyr` (males: Charon, Enceladus, Umbriel). Custom pronunciation not supported for ro-RO; pace and pauses are |
| Warm en female voices | Many library voices; names not verified here (choose in web studio) | Standard: `en-US-AvaNeural`, `EmmaNeural`, `JennyNeural` (has a `friendly` style), `AriaNeural` (`friendly`, `chat`). HD: `en-US-Ava:DragonHDLatestNeural`, `Emma`, `Jenny`, `Phoebe`, `Serena`, `Nova` and others | Chirp 3 HD: `en-US-Chirp3-HD-Aoede`, `Kore`, `Leda`, `Zephyr` (female) |

Sources:
- ElevenLabs: https://elevenlabs.io/pricing , https://elevenlabs.io/terms , https://elevenlabs.io/docs/help-center/legal/can-i-publish-the-content-i-generate-on-the-platform , https://elevenlabs.io/text-to-speech/romanian
- Azure: https://azure.microsoft.com/en-us/pricing/details/cognitive-services/speech-services/ , https://learn.microsoft.com/en-us/azure/ai-services/speech-service/language-support?tabs=tts , https://learn.microsoft.com/en-us/legal/cognitive-services/speech-service/tts-code-of-conduct ; third-party price figures: https://texttolab.com/blog/azure-text-to-speech-pricing
- Google: https://cloud.google.com/text-to-speech/pricing , https://docs.cloud.google.com/text-to-speech/docs/chirp3-hd , https://cloud.google.com/terms/service-terms

Points to remember: the ElevenLabs free plan cannot ship in the app, so a paid plan is needed before production audio, but the audition itself on the free web studio is fine. Azure and Google both have a free monthly quota far above what about 600 short lines per language needs (roughly 40k to 60k characters), so cost for either is effectively zero. Azure and Google terms were not checked for a child-directed-app clause beyond the pages named above; a COPPA review of any chosen vendor belongs to the privacy work, not this spike. Generated audio shipped in the APK sends no child data to the vendor (lines are generated offline by us).

### Test lines (six, exact)

| key | en | ro |
|---|---|---|
| greeting | Hello! I'm Eva. I'm so happy to see you. Let's play together! | Bună! Eu sunt Eva. Mă bucur atât de mult să te văd. Hai să ne jucăm împreună! |
| retry | Oops, not quite. Try again, you can do it! | Hopa, nu chiar. Mai încearcă o dată, tu poți! |
| cheer | Wonderful! You did it! Here are your coins. | Minunat! Ai reușit! Uite monedele tale. |

## Decision

Google Chirp 3 HD, voice Leda: `en-US-Chirp3-HD-Leda` for English and `ro-RO-Chirp3-HD-Leda` for Romanian. Reasons: the user heard it and liked it, the same voice name exists in both languages so Eva is expected to sound like one person (not yet heard in Romanian), and the free quota covers the whole job.

### Cost estimate

About 600 lines per language, assuming about 60 characters per line, is about 36,000 characters per language. Google gives the first 1 million Chirp 3 HD characters per month free, so one language uses about 3.6 percent of one month's free quota, and even ten languages generated in the same month (about 360,000 characters) would stay inside it. After the free quota Chirp 3 HD costs 30 US dollars per 1 million characters, so a full 36,000-character language would cost about 1.08 dollars if it were billed. Re-generating lines after script edits counts against the same quota. Expected cost: zero.

### Caveats and follow-ups

- Romanian pronunciation and diacritics with Leda are not yet confirmed by ear (see Verdict). Check them at the first Romanian voice generation in M4, with the three test lines below. If Leda is unsatisfactory in Romanian, Aoede, Kore and Zephyr exist in ro-RO as well, but that would give Romanian a different voice from English.
- The vendor terms table above is kept as researched. Google's terms were not checked for a child-directed clause beyond the pages read; that check belongs to the privacy work (M7, was M6), not this spike.
- Generation later needs a Google Cloud account and an API key. The user creates these themselves; the key is kept in an environment variable and never goes into the repository or a chat message. The production voice-line script is built in M1 (English only); Romanian recording waits for M4.
- ElevenLabs and Azure were not auditioned and are not chosen. If Google is ever dropped, the earlier notes remain: ElevenLabs free plan cannot ship (needs Starter, 6 dollars per month) and Azure has only one Romanian female voice.

### Shortlist as researched before the audition (kept for reference)

1. Google Chirp 3 HD (Aoede, Leda, Kore, Zephyr): same female voice family in ro-RO and en-US, so one consistent Eva in both languages; 1M free characters.
2. ElevenLabs (free web studio for the audition only): usually the most natural and expressive; Romanian supported. Needs Starter ($6) for shipping.
3. Azure: `en-US-Ava:DragonHDLatestNeural` / `JennyNeural` (friendly style) for English, `ro-RO-AlinaNeural` for Romanian; only one female Romanian voice and no HD, so it is the fallback.
