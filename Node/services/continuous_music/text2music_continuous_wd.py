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
from selenium.webdriver.common.action_chains import ActionChains
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
    # print("found these prompts:", prompts)
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


        print_all_prompt_volumes()
        set_prompt_volume_percent("calm", 10) 
        pairs = get_all_prompt_volumes()
        print(">>> current prompt-volume:", pairs)
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

def get_prompt_text(container):
    try:
        return container.find_element(By.CSS_SELECTOR, "div.kWfOUR").text.strip()
    except Exception:
        # 兜底：找没有子元素、只有文本的 div
        return container.find_element(By.XPATH, ".//div[not(*) and normalize-space()]").text.strip()

def get_all_prompt_volumes():
    results = []
    # 等待至少有一个 trackContainer
    WebDriverWait(driver, 10).until(
        lambda d: len(d.find_elements(By.CSS_SELECTOR, "div.trackContainer")) > 0
    )

    containers = driver.find_elements(By.CSS_SELECTOR, "div.trackContainer")
    for c in containers:
        try:
            prompt = get_prompt_text(c)
            # 该 track 下的 slider
            slider = c.find_element(By.XPATH, ".//span[@role='slider' and @aria-valuenow]")
            vol_str = slider.get_attribute("aria-valuenow")  # 可能是 '0.5' 或 '35'
            results.append((prompt, vol_str))
        except Exception as e:
            print(f">*Ubiq*<抓取某条 prompt 失败: {e}")
    return results

def norm_vol(v):
    if isinstance(v, str):
        v = v.strip().rstrip('%')
    try:
        f = float(v)
    except:
        return None
    if f <= 1:
        f *= 100
    return max(0, min(100, int(round(f))))

def _find_container_by_prompt(prompt_text: str):
    target = prompt_text.strip()
    containers = driver.find_elements(By.CSS_SELECTOR, "div.trackContainer")
    for c in containers:
        try:
            t = c.find_element(By.CSS_SELECTOR, "div.kWfOUR").text.strip()
        except:
            try:
                t = c.find_element(By.XPATH, ".//div[not(*) and normalize-space()]").text.strip()
            except:
                continue
        if t == target:
            return c
    return None

def slide_prompt_volume(prompt, percent):
    """
    通过拖动滑块设置指定 prompt 的音量（0~100 / 0~1 都支持）
    """
    vol = norm_vol(percent)
    if vol is None:
        print(f">*Ubiq*<非法音量: {percent}")
        return False

    try:
        # 1. 找到这个 prompt 的 container
        container = _find_container_by_prompt(prompt)
        if not container:
            print(f">*Ubiq*<没找到 '{prompt}' 的 container")
            return False

        # 2. 找滑轨 & slider & thumb
        track  = container.find_element(By.XPATH, ".//span[@data-orientation='horizontal' and @aria-disabled='false']")
        slider = container.find_element(By.XPATH, ".//span[@role='slider' and @aria-valuenow]")
        thumb  = container.find_element(By.XPATH, ".//span[contains(@style,'--radix-slider-thumb-transform')]")

        # 3. 计算当前位置与目标位置（像素）
        cur = norm_vol(slider.get_attribute("aria-valuenow"))
        mn  = norm_vol(slider.get_attribute("aria-valuemin") or 0)
        mx  = norm_vol(slider.get_attribute("aria-valuemax") or 100)
        print("min", mn, "max", mx)

        rect_track = track.rect
        width = rect_track['width']
        # 起点 & 终点（相对 track 左侧的像素）
        start_px  = (cur - mn) / (mx - mn) * width
        target_px = (vol - mn) / (mx - mn) * width
        delta_x   = target_px - start_px
        if abs(delta_x) < 1:
            print(f">*Ubiq*<'{prompt}' 已经是 {vol}%")
            return True

        # 4. 用 ActionChains 拖动
        actions = ActionChains(driver)
        # 先把鼠标移动到 track 左上角偏移 start_px 的位置
        actions.move_to_element_with_offset(track, start_px, rect_track['height']/2)
        actions.click_and_hold()
        actions.move_by_offset(delta_x, 0)
        actions.release()
        actions.perform()

        # 校验
        new_val = slider.get_attribute("aria-valuenow")
        print(f">*Ubiq*<为 '{prompt}' 滑动到 {new_val}% (目标 {vol}%)")
        return True
    except Exception as e:
        print(f">*Ubiq*<滑动 '{prompt}' 失败: {e}")
        return False
    
def _dump_all_prompts():
    js = r"""
    const res = [];
    document.querySelectorAll('span[role="slider"][aria-valuenow]').forEach((sl,i)=>{
      const cont = sl.closest('div.trackContainer');
      let txt = '';
      if (cont) {
        const t1 = cont.querySelector('div.kWfOUR');
        if (t1) {
          txt = t1.textContent.trim();
        } else {
          // 找叶子 div
          const divs = cont.querySelectorAll('div');
          for (const d of divs) {
            if (d.children.length === 0 && d.textContent.trim()) {
              txt = d.textContent.trim();
              break;
            }
          }
        }
      }
      res.push({
        idx: i,
        text: txt,
        now: sl.getAttribute('aria-valuenow'),
        min: sl.getAttribute('aria-valuemin'),
        max: sl.getAttribute('aria-valuemax')
      });
    });
    return res;
    """
    return driver.execute_script(js)

def print_all_prompt_volumes():
    data = _dump_all_prompts()
    for d in data:
        print(f"[{d['idx']}] '{d['text']}' now={d['now']} min={d['min']} max={d['max']}")


def set_prompt_volume_percent(prompt: str, percent):
    """
    percent: 0~100 的百分比（整数/浮点/字符串都行）
             0  -> min (比如 0)
             100-> max (比如 2)
             25 -> 映射到 0~2 范围的 0.5
    """
    # 先在 Python 端 clamp，避免 NaN
    try:
        pct = float(str(percent).strip().rstrip('%'))
    except Exception:
        print(f">*Ubiq*<非法百分比: {percent}")
        return None
    pct = max(0.0, min(100.0, pct))

    js = r"""
    return (function(name, pct){
      // 1. 找到包含该 prompt 的 trackContainer
      const containers = [...document.querySelectorAll('div.trackContainer')];
      const cont = containers.find(c=>{
        const t = c.querySelector('div.kWfOUR') || c.querySelector("div:not(:has(*))");
        return t && t.textContent.trim() === name.trim();
      });
      if(!cont) return 'notfound';

      // 2. 找 slider 元素
      const sliderEl = cont.querySelector('span[role="slider"][aria-valuenow]');
      if(!sliderEl) return 'no slider';

      // 3. 读 min/max按百分比换算真实值
      const vmin = parseFloat(sliderEl.getAttribute('aria-valuemin') || '0');
      const vmax = parseFloat(sliderEl.getAttribute('aria-valuemax')  || '100');
      const realVal = vmin + (vmax - vmin) * (pct/100);

      // 4. React Fiber 寻找 onValueChange
      const key = Object.keys(sliderEl).find(k => k.startsWith('__reactFiber$') || k.startsWith('__reactProps$'));
      if(!key) return 'no fiber key';
      let fiber = sliderEl[key];
      let handler = null;
      while(fiber){
        const props = fiber.memoizedProps;
        if(props && typeof props.onValueChange === 'function'){ handler = props.onValueChange; break; }
        fiber = fiber.return;
      }
      if(!handler) return 'no handler';

      // 5. 调用 handler 设置
      handler([realVal]);

      return {match: name, min:vmin, max:vmax, setPercent:pct, setReal:realVal};
    })(arguments[0], arguments[1]);
    """
    result = driver.execute_script(js, prompt, pct)
    print(f">*Ubiq*<set_prompt_volume_percent('{prompt}', {pct}) -> {result}")
    return result


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
