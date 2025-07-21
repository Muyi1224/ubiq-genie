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
import threading
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
import time

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

        self._last_ts = 0.0
        self._last_content = ""

    def on_modified(self, event):
        if event.src_path == FILE_PATH:
            if event.src_path == FILE_PATH:
                now = time.time()
                # 如果距离上次处理不到 0.5 秒，就忽略
                if now - self._last_ts < 0.5:
                    return
                self._last_ts = now
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

    # current = get_ui_prompts()
    # print(">>> current prompts：", current)
    
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

        # delete_prompt_by_text("soothing chilled piano")
        # —— 最后执行 delete_queue 中挂起的删除请求 ——
        with delete_lock:
            while delete_queue:
                keyword = delete_queue.pop(0)
                delete_prompt_by_text(keyword)

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



    # if chunk_counter % 450 == 0:  # 每秒1次能量打印（避免刷屏卡顿）
    #     sys.stderr.write(f"[Energy] {volume:.1f}\n")

    if volume > THRESHOLD:
        # ① 重采样到 48 kHz
        resampled_audio_data = scipy.signal.resample(audio_data* volume_factor, int(len(audio_data) * (48000 / 96000))).astype(np.int16)
        # TBD
        # sys.stdout.buffer.write(np.int16(resampled_audio_data))
        # ② 把 numpy 转原始 bytes
        pcm_bytes = resampled_audio_data.tobytes()

        # ③ 先发一个 JSON 头（一次 clip 发一次即可；此处简单每帧都发）
        # send_audio_header(len(pcm_bytes))

        # # ④ 按 16 000 B 切包输出到 stdout，Node 直接读取
        # PACK = 16_000
        # for i in range(0, len(pcm_bytes), PACK):
        #     chunk = pcm_bytes[i:i+PACK]
        #     sys.stdout.buffer.write(chunk)
        #     sys.stdout.buffer.flush()

def send_audio_header(total_bytes: int):
    hdr = {
        "type": "AudioInfo",
        "targetPeer": "Music Service",
        "audioLength": total_bytes
    }
    sys.stdout.write(json.dumps(hdr) + "\n")    # 仍走 stdout（文本）
    sys.stdout.flush()


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

def get_ui_prompts():
    prompts = []
    # 1) Retrieve all top-level prompt containers
    containers = driver.find_elements(By.CSS_SELECTOR, "div.trackContainer")
    for c in containers:
        try:
            # 2) Locate the text-only <div> within the container (assumes class "kWfOUR")
            text_div = c.find_element(By.CSS_SELECTOR, "div.kWfOUR")
            prompts.append(text_div.text.strip())
        except:
            # Fallback: find a <div> that has no child elements and contains text
            text_div = c.find_element(
                By.XPATH, ".//div[not(*) and normalize-space()]"
            )
            prompts.append(text_div.text.strip())
    return prompts

def delete_prompt_by_text(keyword):
    try:
        # Re-fetch all prompt containers and delete buttons
        containers     = driver.find_elements(By.CSS_SELECTOR, "div.trackContainer")
        delete_buttons = driver.find_elements(By.CSS_SELECTOR, "button.deleteButton")

        # Iterate through each container to find the one matching the keyword
        for idx, container in enumerate(containers):
            # Attempt to locate the text element by its CSS class
            try:
                text_div = container.find_element(By.CSS_SELECTOR, "div.kWfOUR")
            except:
                # Fallback: find a div that contains only text (no child elements)
                text_div = container.find_element(
                    By.XPATH, ".//div[not(*) and normalize-space()]"
                )

            # Check if the prompt text matches the keyword exactly
            if text_div.text.strip() == keyword:
                # Click the corresponding delete button by index
                if idx < len(delete_buttons):
                    driver.execute_script(
                        "arguments[0].click();",
                        delete_buttons[idx]
                    )
                    print(f">*Ubiq*<Clicked delete for prompt '{keyword}' at index {idx}")
                    return True
                else:
                    print(f">*Ubiq*<Found '{keyword}' at idx={idx} but no matching delete button.")
                    return False

        # If no matching prompt was found
        print(f">*Ubiq*<Prompt '{keyword}' not found on page; no delete performed.")
        return False

    except Exception as e:
        # Catch any unexpected errors during the deletion process
        print(f">*Ubiq*<Error deleting prompt '{keyword}': {e}")
        return False

# 全局队列和锁
delete_queue = []
delete_lock  = threading.Lock()

def listen_from_node():
    """
    这个线程一直跑，读 stdin，
    收到 DeletePrompt 就把 prompt push 进 delete_queue。
    """
    for raw in sys.stdin:
        print(f"[DEBUG] raw repr: {raw!r}")
        line = raw.lstrip('\ufeff').strip()
        if not line:
            continue
        try:
            msg = json.loads(line)
            if msg.get("type") == "DeletePrompt":
                keyword = msg.get("prompt","").strip()
                if keyword:
                    with delete_lock:
                        delete_queue.append(keyword)
                    print(f">*Ubiq*<Queued delete for prompt: {keyword}")
        except Exception as e:
            print(f"[From Node] JSON error: {e}")




if __name__ == "__main__":

    threading.Thread(target=listen_from_node, daemon=True).start()

    parser = argparse.ArgumentParser(description="Text-to-Sound")
    parser.add_argument("--prompt_postfix", type=str, default="", help="Postfix to add to the prompt.")
    args = parser.parse_args()

    chrome_options = Options()
    chrome_options.add_experimental_option("debuggerAddress", "127.0.0.1:9222")
    chrome_options.add_argument("--mute-audio")

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
