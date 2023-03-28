# Simple GPT Toolkit

With this pack of demos you should be able to hit the ground running, if you're interested in making a game powered by OpenAI's GPT.

### Getting started

**1.** You need an API key from OpenAI, which are available to everyone for free. If you don't have an OpenAI account, you'll have sign up on  **[their website](https://openai.com/api/)**. You can also sign in with a Google or Microsoft account, if you don't want a separate account.

**2.** After you're logged in, click on 'Personal' in the top right corner of the page. Then click on 'View API keys' on the drop down.

**3.** Click on the 'Create new secret key' to generate an API key. You'll get a popup with the key, which you can just copy and paste to the editor.

**4.** When you have the API key on your clipboard, you can hop back into Unity. In Unity, open the 'OpenAI API Key' window from the 'Window' drop down.

**5.** Paste the API key into this window and click the Save button. *This will only save the API key locally, to PlayerPrefs.* You can check out what the clicking the Save button does [here](https://github.com/enkomat/Simple-GPT-Demos/blob/main/Assets/Simple%20GPT%20Toolkit/Editor/OpenAIApiKey.cs). More on API key safety [here](https://help.openai.com/en/articles/5112595-best-practices-for-api-key-safety).

**6.** Install [TextMesh Pro](https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/manual/index.html) if you don't have it in your project already.

**7.** Open up any of the included demo scenes. When you start running any of the scenes, the system will check if OpenAI's GPT API is working properly.

### Explanations of included scripts

**1. SimpleGPT** is the core script. It makes the API requests to GPT and can be used from any outside class to easily make text generation requests. The core method is ChatRequest, which sends the request. The other methods in the class are mostly just to set up the request to be proper for the current context. Every scene that uses GPT should include this script.

**2. SimpleGPTAutoDialog** is a script that sets up the necessary stuff you need to be able to have dialog with NPCs in your game. You can just drop this into the scene, most preferably in the same GameObject as you have placed the SimpleGPT script. In the editor, you can see three fields: FirstPersonMode, Prompt, PromptKey and MaxPromptLength. Activate FirstPersonMode if your game is played in first person, or any other reason you might want to control activating the player prompt with the chosen prompt key only. Prompt is the reference to the player prompt text field itself. PromptKey is the chosen key you use for controlling the prompt. MaxPromptLength is the maximum length of the text the player can send.

**3. SimpleGPTCharacter** is a script you attach to any NPC you want to chat with in your game world. You can add the character's name and description, which are then used to build out the character for the GPT. This information is essentially added to the requests you send to GPT, and GPT is able to act out the character based on that.

**4. SimpleGPTPlayer** very close to the above, but should only be attached to the player. This is how GPT knows your character's role in the world you're building.