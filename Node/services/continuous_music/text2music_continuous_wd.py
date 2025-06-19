from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler
import sys
import pyaudio
import pyaudiowpatch as pyaudio
import os
import json
import argparse
import numpy as np
import wave
import scipy.signal
from selenium import webdriver
from selenium.webdriver.common.by import By
from selenium.webdriver.common.keys import Keys
from selenium.webdriver.chrome.options import Options
from selenium.webdriver.chrome.service import Service
from selenium.webdriver.support.ui import WebDriverWait
from selenium.webdriver.support import expected_conditions as EC
import re

from pathlib import Path

# File to watch
# FILE_PATH = os.path.abspath("../../apps/continuous_music/data/gpt.txt")
FILE_PATH = r"C:\Users\Administrator\Desktop\ubiq-genie\Node\apps\continuous_music\data\gpt.txt"


# Audio stream configuration
FORMAT = pyaudio.paInt16
CHANNELS = 1
RATE = 48000
CHUNK = 1024
THRESHOLD = 100

playFirst = False


class FileWatcher(FileSystemEventHandler):
    """
    Watches for changes in the specified file and processes its contents.
    """
    def __init__(self, callback_prompts, callback_volume):
        self.callback_prompts = callback_prompts
        self.callback_volume = callback_volume

    def on_modified(self, event):
        if event.src_path == FILE_PATH:
            self.process_file()

    def process_file(self):
        try:
            with open(FILE_PATH, "r", encoding="utf-8") as file:
                content = file.read()
                # Extract prompts within square brackets
                prompts = re.findall(r'\[([^\]]+)\]', content)
                # Extract volume value within curly brackets
                volume_matches = re.findall(r'\{(\d+)\}', content)
                volume = int(volume_matches[0]) if volume_matches else None
                self.callback_prompts(prompts)
                if volume is not None:
                    self.callback_volume(volume)
        except Exception as e:
            print(f"Error reading file: {e}")


def ui_loop(prompts):
    global driver, wait, playFirst
    print("found these prompts:", prompts)
    for prompt in prompts:
        try:
            # Add new prompt
            #add_prompt = wait.until(EC.element_to_be_clickable((By.CSS_SELECTOR, "input[placeholder='Add a prompt ...']")))
            # add_prompt = WebDriverWait(driver, 10).until(EC.element_to_be_clickable((By.CSS_SELECTOR, "input[placeholder='Add a prompt …']")))
            add_prompt = WebDriverWait(driver, 10).until(EC.presence_of_element_located((By.TAG_NAME, "input")))
            add_prompt.clear()
            add_prompt.send_keys(prompt)
            add_prompt.send_keys(Keys.RETURN)
            print(f">*Ubiq*<Added new prompt: '{prompt}'")

            # Locate delete buttons
            delete_buttons = driver.find_elements(By.CSS_SELECTOR, "button.deleteButton")
            if len(delete_buttons) > 3:  # Ensure at least 3 prompts remain
                driver.execute_script("arguments[0].click();", delete_buttons[0])
                print(f">*Ubiq*<Clicked the first delete button.")
        except Exception as e:
            print(f"Error in UI loop: {e}")

    try:
        # Click the play button for the first prompt
        if not playFirst:
            #play_button = wait.until(EC.element_to_be_clickable((By.ID, "playButton")))
            play_button = WebDriverWait(driver, 10).until(EC.element_to_be_clickable((By.ID, "playButton")))
            driver.execute_script("arguments[0].click();", play_button)
            print(f">*Ubiq*<Clicked the play button.")
            playFirst = True
    except Exception as e:
        print(f"Error in UI play: {e}")


def set_volume(value):
    """
    Adjusts the volume factor based on the given value.
    :param value: Integer between 0 and 100 representing the desired volume level.
    """
    global volume_factor
    volume_factor = value / 100.0
    print(f">*Ubiq*<Volume factor set to: {volume_factor}")



def generate_music_from_prompt(data, chunk_counter):
    global volume_factor
    audio_data = np.frombuffer(data, dtype=np.int16)
    volume = np.sqrt(np.mean(np.abs(audio_data ** 2)))

    if volume > THRESHOLD:
        resampled_audio_data = scipy.signal.resample(audio_data* volume_factor, int(len(audio_data) * (48000 / 96000)))
        # TBD
        # sys.stdout.buffer.write(np.int16(resampled_audio_data))


def recognize_from_file():
    """
    Starts the file watcher and audio generation loop.
    """
    observer = Observer()
    file_watcher = FileWatcher(ui_loop, set_volume)
    print(f">*Ubiq*<Watching directory: {os.path.dirname(FILE_PATH)}")
    observer.schedule(file_watcher, path=os.path.dirname(FILE_PATH), recursive=False)
    observer.start()

    try:
        print(f">*Ubiq*<Watching file: {FILE_PATH}")
        chunk_counter = 0
        while True:
            data = stream.read(CHUNK)
            generate_music_from_prompt(data, chunk_counter)
            chunk_counter += 1
    except KeyboardInterrupt:
        observer.stop()
    except Exception as e:
        print(f"An error occurred: {e}")
    finally:
        observer.join()


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Text-to-Sound")
    parser.add_argument("--prompt_postfix", type=str, default="", help="Postfix to add to the prompt.")
    args = parser.parse_args()

    chrome_options = Options()
    chrome_options.add_experimental_option("debuggerAddress", "127.0.0.1:9222")
    # service = Service("D:/Applications/chromedriver-win64/chromedriver.exe")
    chromedriver_path = Path("D:/Google Download/chromedriver-win64/chromedriver-win64/chromedriver.exe")
    service = Service(str(chromedriver_path))

    driver = webdriver.Chrome(service=service, options=chrome_options)

    audio = pyaudio.PyAudio()

    try:
        wasapi_info = audio.get_host_api_info_by_type(pyaudio.paWASAPI)
    except OSError:
        print("WASAPI is not available on the system. Exiting.")
        sys.exit()

    default_speakers = audio.get_device_info_by_index(wasapi_info["defaultOutputDevice"])

    if not default_speakers["isLoopbackDevice"]:
        for loopback in audio.get_loopback_device_info_generator():
            if default_speakers["name"] in loopback["name"]:
                default_speakers = loopback
                break
        else:
            print("Default loopback output device not found. Exiting.")
            sys.exit()

    stream = audio.open(
        format=FORMAT,
        channels=default_speakers["maxInputChannels"],
        rate=int(default_speakers["defaultSampleRate"]),
        frames_per_buffer=CHUNK,
        input=True,
        input_device_index=default_speakers["index"]
    )

    driver.get("https://aitestkitchen.withgoogle.com/tools/music-fx-dj")
    print(f">*Ubiq*<Navigated to MusicFX DJ page.")

    wait = WebDriverWait(driver, 30)
    volume_factor = 1.0
    recognize_from_file()

#launch this before
# "C:\Program Files\Google\Chrome\Application\chrome.exe" --remote-debugging-port=9222 --user-data-dir="C:\chromedriver_profile"  --unsafely-disable-devtools-self-xss-warnings &


"""
const xpath = "/html/body/div[3]/div/span[1]/span[2]/span";
const sliderElement = document.evaluate( xpath,  document,   null,   XPathResult.FIRST_ORDERED_NODE_TYPE,   null).singleNodeValue;
sliderElement.setAttribute('aria-valuenow', '30'); // Change value to desired level

const sliderContainer = document.evaluate('/html/body/div[3]/div/span[1]/span[2]', document, null, XPathResult.FIRST_ORDERED_NODE_TYPE,null).singleNodeValue;
sliderContainer.style.left = `calc(${percentage}%)`;

"""
