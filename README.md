Gameplay
One-player CYOA (Choose Your Own Adventure) game with infinite AI-generated content and a shared marketplace. 

Rules
- Players gain 1 Level per run
- 1 run costs 1 Heart
- Players can buy 3 Hearts for 1 Dollar if they run out (free for now)
- Players power up with Items from successful runs

Content generation
We start with a random theme from a starting pool of 3 genres (Adventure, History, or Fairy Tail), mash the story up with SUPERCELL IP, and add 2 level design Parameters: Player Level and Items (we use the Clash of Clans and Clash Royal IP to keep stories within a generic universe of role-based characters, e.g. The Prince vs. Leo). Items can be anything, for example Green Gem, Barbarian Sword, Statue of Liberty etc. [Link]

Prompt example (image, Gemini 3)
Make a 1024x1024 image black-and-white Choose Your Own Adventure style illustration set in the Clash of Clans universe for the [INJECT_GENRE] genre (no text, single panel) for the following scene: [INJECT_SCENE_DESCRIPTION]. [Link]
(sample scene description: You wake up in a dimly lit forest clearing. The air smells of pine and damp earth. A narrow trail leads north, and you can hear running water to the east.)

Prompt example (image, ChatGPT 5.2 Pro)
Create the book cover art for a Choose Your Own Adventure (i.e. retro, throwback style) with the SUPERCELL IP in the [INJECT_GENRE] genre. [Link]
(sample genre: History)

Next tasks
- Hearts refresh at 1 Heart per hour (3 Hearts max)
- Run difficulty and Item strength grow with Level
- Generate and serve content with a real backend 
- Expand themes generatively after Level 3
- More visual/stylistic difference between themes
- Players may trade items in the Marketplace

Prompt setup files
supercell-ip.txt [Link]
make-adventure.txt [Link]

AI acknolwdgements
Cursor with ChatGPT 5.2 Codex for Unity
Google Gemini 3 for image creation
Claude 4.6 for code development
ChatGPT 5.2 for script automation
DeeVid for image to video (https://deevid.ai)

Interesting notes
The color concept (feature that links items accross adventures e.g. Yellow Tablet) was genertated on its own by the AI (Google Gemini 3)
