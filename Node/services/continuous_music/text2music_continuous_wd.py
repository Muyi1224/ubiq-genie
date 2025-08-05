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

debug_log = open("debug.txt", "w", buffering=1)
def debug(msg):
    debug_log.write(f"{time.time()}: {msg}\n")
    debug_log.flush()

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
        # set_prompt_volume_percent("calm", 10) 
        with volume_lock:
            while volume_queue:
                kw, vol = volume_queue.pop(0)
                set_prompt_volume_percent(kw, vol)

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
                    debug(f">*Ubiq*<Clicked delete for prompt '{keyword}' at index {idx}")
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
volume_queue = []
delete_lock  = threading.Lock()
volume_lock  = threading.Lock()
    

def _is_button_pressed(btn):
    """
    返回 True=已静音, False=未静音, None=无法判断
    """
    aria = btn.get_attribute("aria-pressed")
    if aria is not None:
        return aria.lower() == "true"

    cls = (btn.get_attribute("class") or "").lower()
    if "active" in cls or "toggled" in cls:
        return True
    if "inactive" in cls:
        return False
    return None


def ensure_mute_state(prompt_text: str, target_mute: bool, max_retry=3, wait_sec=0.15):
    """
    点击 muteButton 直到按钮状态 == target_mute
    """
    try:
        cont = _find_container_by_prompt(prompt_text)
        if not cont:
            print(f">*Ubiq*<找不到 prompt '{prompt_text}' 的 container")
            return False

        btn = cont.find_element(By.CSS_SELECTOR, "button.muteButton")

        for attempt in range(max_retry):
            cur_state = _is_button_pressed(btn)
            if cur_state == target_mute:
                print(f">*Ubiq*<'{prompt_text}' 已是 mute={target_mute} (尝试 {attempt})")
                return True

            # 状态未知或不符 → 点击一次再等
            driver.execute_script("arguments[0].click();", btn)
            time.sleep(wait_sec)

        # 尝试多次后仍未达到
        final_state = _is_button_pressed(btn)
        print(f">*Ubiq*<⚠️ 无法将 '{prompt_text}' 设为 mute={target_mute}，最终状态={final_state}")
        return False

    except Exception as e:
        print(f">*Ubiq*<点击 muteButton 失败: {e}")
        return False



def listen_from_node():
    for raw in sys.stdin:
        print(f"[DEBUG] raw repr: {raw!r}")
        line = raw.lstrip('\ufeff').strip()
        if not line:
            continue
        try:
            msg = json.loads(line)
            mtype = msg.get("type")

            # ----- 删除指定 prompt -----------------------------------------
            if mtype == "DeletePrompt":
                keyword = msg.get("prompt", "").strip()
                if keyword:
                    with delete_lock:
                        delete_queue.append(keyword)
                    print(f">*Ubiq*<Queued delete for prompt: {keyword}")
                    debug(f">*Ubiq*<Queued delete for prompt: {keyword}")

            # ----- 调整指定 prompt 的音量 ----------------------------------
            elif mtype == "SetPromptVolume":
                prompt  = msg.get("prompt", "").strip()
                volume  = msg.get("volume")          # 0~100 的整数
                if prompt and volume is not None:
                    with volume_lock:
                        volume_queue.append((prompt, volume))
                    print(f">*Ubiq*<Queued volume {volume} for '{prompt}'")
                    debug(f">*Ubiq*<Queued volume {volume} for '{prompt}'")

            elif mtype == "Mute":
                prompt = msg.get("prompt", "").strip()
                target = bool(msg.get("mute"))  # true=静音
                if prompt:
                    ensure_mute_state(prompt, target)

            elif mtype in ("density", "brightness", "chaos"):
                level = (msg.get("level") or
                        msg.get("density") or  
                        "").strip().lower()     # "auto" / "low" / "high"

                if not level:
                    print(f">*Ubiq*<⚠️ {mtype} 缺少 level")
                    continue

                if mtype == "density":
                    ensure_density(level)
                elif mtype == "brightness":
                    ensure_brightness(level)
                else:                            # SetChaos
                    ensure_chaos(level)

                debug(f">*Ubiq*<执行 {mtype} → {level}")
            
            elif mtype == "bpm":
                ensure_bpm(msg.get("value", "auto"))
            
            elif mtype == "TrackMute":
                track = msg.get("track", "").strip().lower()   # drums / bass / other
                target = bool(msg.get("mute"))                 # True=静音
                if track in ("drums", "bass", "other"):
                    ensure_track_mute(track, target)




        except Exception as e:
            print(f"[From Node] JSON error: {e}")

            
def get_prompt_text(container):
    try:
        return container.find_element(By.CSS_SELECTOR, "div.kWfOUR").text.strip()
    except Exception:
        # 兜底：找没有子元素、只有文本的 div
        return container.find_element(By.XPATH, ".//div[not(*) and normalize-space()]").text.strip()

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
    
def print_all_prompt_volumes():
    js = """
    return [...document.querySelectorAll('span[role="slider"][aria-valuenow]')].map(sl=>{
      const cont = sl.closest('div.trackContainer');
      let txt='';
      if(cont){
        const t1 = cont.querySelector('div.kWfOUR');
        if(t1) txt=t1.textContent.trim();
        else{
          const divs=cont.querySelectorAll('div');
          for(const d of divs){ if(d.children.length===0 && d.textContent.trim()){txt=d.textContent.trim();break;}}
        }
      }
      return {text:txt, now:sl.getAttribute('aria-valuenow'),
              min:sl.getAttribute('aria-valuemin'), max:sl.getAttribute('aria-valuemax')};
    });
    """
    data = driver.execute_script(js)
    for i,d in enumerate(data):
        print(f"[{i}] '{d['text']}' now={d['now']} min={d['min']} max={d['max']}")



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
    debug(f">*Ubiq*<set_prompt_volume_percent('{prompt}', {pct}) -> {result}")
    return result

# ---------- 通用内部工具 ---------- #
def _find_button(prefix: str, level: str):
    """
    优先按 id 查找  <button id="density_auto">…
    如果前端改了 id，再退而按 data-motion or label 文本模糊匹配
    """
    # 1) id 方式（最快最稳）
    try:
        return driver.find_element(By.ID, f"{prefix}_{level}")
    except Exception:
        pass

    # 2) 按 data-motion 属性（Google MusicFX 页面会带 data-motion-pop-id）
    try:
        return driver.find_element(
            By.CSS_SELECTOR,
            f'button[data-motion-pop-id*="{prefix}"][data-motion-pop-id*="{level}"]'
        )
    except Exception:
        pass

    # 3) 最后兜底：在同容器(label)下通过顺序查找
    try:
        label_texts = {
            "density": ("density", "密度"),
            "brightness": ("brightness", "亮度"),
            "chaos": ("chaos", "混乱", "随机")   # 可能翻译不同
        }
        label_words = label_texts.get(prefix, ())
        # 找到写着“密度/亮度/混乱”等字样的 label，然后在同级下拿 button 列表
        for word in label_words:
            lab = driver.find_element(By.XPATH, f"//label[contains(text(), '{word}')]")
            # 同一个父节点里第 1 / 2 / 3 个按钮即 auto/low/high
            btns = lab.find_elements(By.XPATH, ".//preceding::button")[-3:]
            idx  = {"auto":0, "low":1, "high":2}[level]
            return btns[idx]
    except Exception:
        pass
    return None


def _is_selected(button):
    aria = button.get_attribute("aria-pressed")
    if aria:
        return aria.lower() == "true"
    cls = (button.get_attribute("class") or "").lower()
    return "active" in cls or "selected" in cls or "toggled" in cls


def _ensure(prefix: str, level: str, retry=3, wait=0.15):
    level = level.lower().strip()
    if level not in ("auto", "low", "high"):
        debug(f">*Ubiq*<非法 level '{level}' for {prefix}")
        return False

    btn = _find_button(prefix, level)
    if not btn:
        debug(f">*Ubiq*<找不到按钮 {prefix}_{level}")
        return False

    for _ in range(retry):
        if _is_selected(btn):
            debug(f">*Ubiq*<{prefix} 已是 {level}")
            return True
        driver.execute_script("arguments[0].click();", btn)
        time.sleep(wait)

    debug(f">*Ubiq*<无法把 {prefix} 切到 {level}")
    return False

def ensure_density(level: str, max_retry=3, wait_sec=0.15):
    return _ensure("density", level, max_retry, wait_sec)

def ensure_brightness(level: str, max_retry=3, wait_sec=0.15):
    return _ensure("brightness", level, max_retry, wait_sec)

def ensure_chaos(level: str, max_retry=3, wait_sec=0.15):
    # “混乱 / 随机度” 我用 chaos 前缀；若前端实际 id 用 randomness，自行改成 randomness
    return _ensure("chaos", level, max_retry, wait_sec)

# 60–180 BPM 约束
# === 60–180 BPM =========================================================
BPM_MIN = 60
BPM_MAX = 180

# —— 1. 找顶部 BPM 按钮 ——————————————————————————————
def _find_bpm_button():
    """
    返回「顶部 BPM 按钮」的 WebElement  
    • 先找 aria-haspopup="dialog" 且内部有 <small>BPM</small>  
    • 再兜底按文字 “BPM” 匹配
    """
    XPATH_BTN = (
        # ① 你的截图：<button aria-haspopup="dialog"> <small>BPM</small> <p>102</p> …
        "//button[@aria-haspopup='dialog' and .//small[normalize-space()='BPM']]"
        # ② 若上面失败，再模糊找含 BPM 的按钮
        " | //button[contains(.,'BPM')]"
    )
    return driver.find_element(By.XPATH, XPATH_BTN)

from selenium.common.exceptions import TimeoutException

def _wait_bpm_panel(btn, timeout=5):
    pid = btn.get_attribute("aria-controls")
    if not pid:
        raise TimeoutException("BPM button missing aria-controls")
    panel = WebDriverWait(driver, timeout).until(
        EC.visibility_of_element_located((By.ID, pid))
    )
    track = panel.find_element(By.CSS_SELECTOR,
                               "span[data-orientation='horizontal']")
    return panel, track  # track 里第一个 <span> 就是 thumb


def ensure_bpm(value):
    debug(f"[BPM] enter ensure_bpm({value})")

    # ① 打开面板
    try:
        top_btn = _find_bpm_button()
        driver.execute_script("arguments[0].click();", top_btn)
    except Exception as e:
        debug(f"[BPM] ❌ click top-btn: {e}")
        return False

    try:
        panel, track = _wait_bpm_panel(top_btn)
    except TimeoutException:
        debug("[BPM] ❌ panel timeout")
        return False

    if str(value).lower().strip() == "auto":
        try:
            panel.find_element(By.XPATH,
                ".//button[normalize-space()='重置' or normalize-space()='Reset']"
            ).click()
            debug("[BPM] reset -> auto")
        finally:
            panel.find_element(By.XPATH,
                ".//button[normalize-space()='应用' or normalize-space()='Apply']"
            ).click()
        return True

    bpm = max(60, min(180, int(round(float(value)))))

    # ② 用 JS 改 thumb transform + aria-valuenow
    js = """
      const bpm   = arguments[0];
      const track = arguments[1];

      const min = parseFloat(track.getAttribute('aria-valuemin') || 60);
      const max = parseFloat(track.getAttribute('aria-valuemax') || 180);
      const ratio = (bpm - min) / (max - min);

      const thumb = track.querySelector('span');
      if(thumb){
          thumb.style.setProperty('--radix-slider-thumb-transform',
                                  `translateX(${ratio*100}%)`);
      }
      track.setAttribute('aria-valuenow', bpm);
    """
    driver.execute_script(js, bpm, track)
    debug(f"[BPM] set to {bpm} via JS")

    # ③ 点应用
    try:
        panel.find_element(By.XPATH,
            ".//button[normalize-space()='应用' or normalize-space()='Apply']"
        ).click()
    except Exception:
        pass

    return True



# ──────────── Track-Mute helpers ────────────
_TRACK_ID_MAP = {
    "drums": "drum-control",
    "bass":  "bass-control",
    "other": "other-control",
}

def _find_track_button(track: str):
    """
    根据 track 名 ("drums" / "bass" / "other") 找到对应按钮 <button id="drum-control">…
    若前端改动了 id，可在 _TRACK_ID_MAP 中自行调整。
    """
    track = track.lower().strip()
    btn_id = _TRACK_ID_MAP.get(track)
    if not btn_id:
        return None
    try:
        return driver.find_element(By.ID, btn_id)
    except Exception:
        return None

def _is_btn_muted(btn) -> bool|None:
    """
    读取按钮当前是否为『静音』。
    先看 aria-pressed，再兜底看 class。
    返回 True=静音，False=未静音，None=无法判断。
    """
    aria = btn.get_attribute("aria-pressed")
    if aria is not None:
        return aria.lower() == "true"
    cls = (btn.get_attribute("class") or "").lower()
    if "active" in cls or "toggled" in cls or "muted" in cls:
        return True
    if "inactive" in cls:
        return False
    return None

def ensure_track_mute(track: str, target_mute: bool,
                      retry:int = 3, wait:float = 0.15) -> bool:
    """
    反复点击按钮直至 Drums/Bass/Other 达到目标静音状态。

    track       : "drums" | "bass" | "other"
    target_mute : True → 静音, False → 取消静音
    """
    btn = _find_track_button(track)
    if not btn:
        debug(f">*Ubiq*<找不到 track 按钮: {track}")
        return False

    for _ in range(retry):
        cur = _is_btn_muted(btn)
        if cur == target_mute:
            debug(f">*Ubiq*<{track} 已是 mute={target_mute}")
            return True
        driver.execute_script("arguments[0].click();", btn)
        time.sleep(wait)

    debug(f">*Ubiq*<无法将 {track} 置为 mute={target_mute}")
    return False
# ────────────────────────────────────────────


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
