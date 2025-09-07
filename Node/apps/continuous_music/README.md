# Continuous Music Generation Sample

This guide explains how to use the Ubiq-Genie framework with the continuous_music system to generate music. The sample works by automating a browser running in debug mode to access the Google MusicFX DJ tool page. A backend service monitors a text file; when the file's content changes, the new text is sent as a prompt to MusicFX to generate corresponding music.

## Prerequisites

For this sample, you will need a computer that can run Node.js and Python, and you must complete the following setup steps:

1. Install the Ubiq-Genie Framework

> [!IMPORTANT]
> Before proceeding, ensure the Ubiq-Genie framework and the necessary dependencies for this sample are correctly installed. For further details, please see the root [README](../../../README.md) file.

2. Configure Chrome for Remote Debugging
You need to launch Google Chrome in remote debugging mode. Open a terminal (e.g., Command Prompt or PowerShell on Windows) and execute the following command:

"C:\Program Files\Google\Chrome\Application\chrome.exe" ^
--remote-debugging-port=9222 ^
--user-data-dir="C:\chrome_debug_profile" ^
--unsafely-disable-devtools-self-xss-warnings

Note: Ensure your Chrome installation path matches the one in the command above. `C:\chrome_debug_profile` is a temporary user data directory, which you can change to a different path if needed.

3. Open the MusicFX DJ Page
In the Chrome browser that you launched in debug mode, manually navigate to the following URL: https://labs.google/fx/tools/music-fx-dj

## Running the Sample

We recommend using VS Code to run and modify the server application, with the Node folder as the workspace root.

### Server (Node.js)

We recommend using VS Code to run and modify the server application, with the `Node` folder as the workspace root. To run the server application:

1. Open a terminal and navigate to the `Node/apps/continuous_music` directory. Make sure your `conda` or `venv` Python environment is activated.

2. Execute the following command. This will guide you through the configuration process, including setting up server information and required environment variables. Configuration only runs the first time you start the application. Ensure you apply the same server configuration to the Unity client (in `Unity/Assets/ServerConfig.asset`).

    ```bash
    npm start continuous_music
    ```

3. Important Configuration:

The Python script `text2music_continuous.py` needs the exact location of the gpt.txt file. Please check this script and ensure the `FILE_PATH` variable points to the correct path. The default path is:
`FILE_PATH = r"C:\Users\Administrator\Desktop\ubiq-genie\Node\apps\continuous_music\data\gpt.txt"`
If your project is located elsewhere, you must modify this line.

If you need to reconfigure the application, you can run `npm start continuous_music configure`. You can also manually modify the `config.json` (server configuration) and `.env` (environment variables) files.

### Client (Unity)

1. Launch Unity and navigate to the `Unity/Assets/Apps/Base2` directory. Open the `Base2.unity` scene.
2. Ensure the `Room Client` under the `Network Scene` object has the correct IP address and port for the server. If the server is running on the same machine as the Unity Editor, the IP address should be `localhost`. This should correspond to the configuration on the server side.
3. In the Unity Editor, press the `Play` button to launch the application.
4. To generate music, create an object within the Unity scene. This action will update the `gpt.txt` file located at `Node/apps/continuous_music/data/gpt.txt`. The backend watch_dog.py script will detect this change and trigger the `text2music_continuous.py` script. This script reads the new content from `gpt.txt` and uses it as a prompt to generate new music via the automated browser.

## Support

For any questions or issues, please use the Discussions tab on GitHub or send a message in the *ubiq-genie* channel in the [Ubiq Discord server](https://discord.gg/cZYzdcxAAB).
