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
from selenium.common.exceptions import TimeoutException
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

# Global flag to ensure the play button is only clicked once.
playFirst = False

debug_log = open("debug.txt", "w", buffering=1)
def debug(msg):
    # Helper function for writing timestamped debug messages.
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
                # Debounce to prevent rapid firing, ignore if less than 0.5s since last process.
                if now - self._last_ts < 0.5:
                    return
                self._last_ts = now
                self.process_file()

    def process_file(self):
        # Reads the file, parses content, and executes callbacks.
        try:
            with open(FILE_PATH, "r", encoding="utf-8") as file:
                content = file.read()
                # Extract prompts within square brackets
                prompts = re.findall(r'\[([^\]]+)\]', content)
                # Extract volume value within curly brackets
                volume_matches = re.findall(r'\{(\d+)\}', content)
                volume = int(volume_matches[0]) if volume_matches else None
                # Trigger callbacks with parsed data.
                self.callback_prompts(prompts)
                if volume is not None:
                    self.callback_volume(volume)
        except Exception as e:
            print(f"Error reading file: {e}")


def ui_loop(prompts):
    """
    Main UI interaction loop, triggered by file changes.
    Adds new prompts and processes action queues (volume, delete).
    """
    global driver, wait, playFirst
    # current = get_ui_prompts()
    # try:
    #     driver.execute_script("""
    #       if(window.__autoClearObs){ window.__autoClearObs.disconnect(); }
    #       window.__autoClearing = false;
    #     """)
    #     debug("[ClearAll] force disabled before adding prompts")
    # except Exception as e:
    #     debug(f"[ClearAll] force disable error: {e}")

    for prompt in prompts:
        try:
             # Find the input field, clear it, enter the new prompt, and press Enter.
            add_prompt = WebDriverWait(driver, 10).until(EC.presence_of_element_located((By.TAG_NAME, "input")))
            add_prompt.clear()
            add_prompt.send_keys(prompt)
            add_prompt.send_keys(Keys.RETURN)
            print(f">*Ubiq*<Added new prompt: '{prompt}'")

            # Locate delete buttons
            delete_buttons = driver.find_elements(By.CSS_SELECTOR, "button.deleteButton")
            if len(delete_buttons) > 9:  # Ensure at least 9 prompts remain
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


        print_all_prompt_volumes()  # For debugging: print current volumes of all prompts.
        
        # set_prompt_volume_percent("calm", 10) 
        with volume_lock:
            while volume_queue:
                kw, vol = volume_queue.pop(0)
                set_prompt_volume_percent(kw, vol)

        # Process any pending prompt deletions from the queue.
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
    """
    Processes a chunk of audio data, resamples it, and writes to stdout.
    """
    global volume_factor
    audio_data = np.frombuffer(data, dtype=np.int16)
    volume = np.sqrt(np.mean(np.abs(audio_data ** 2)))

    # Only process if audio volume is above the threshold.
    if volume > THRESHOLD:
        # Resample from 96kHz (assumed source) to 48kHz.
        resampled_audio_data = scipy.signal.resample(audio_data, int(len(audio_data) * (48000 / 96000)))
        # Write the processed audio data to standard output.
        sys.stdout.buffer.write(np.int16(resampled_audio_data))


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
    # Retrieve all top-level prompt containers
    containers = driver.find_elements(By.CSS_SELECTOR, "div.trackContainer")
    for c in containers:
        try:
            # Locate the text-only <div> within the container (assumes class "kWfOUR")
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

# Global queues and locks
delete_queue = []
volume_queue = []
delete_lock  = threading.Lock()
volume_lock  = threading.Lock()
    

def _is_button_pressed(btn):
    """
    Helper to check if a button is in a pressed/active state.
    Returns True (pressed), False (not pressed), or None (indeterminate).
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
    Ensures a specific prompt's mute button is in the target state by clicking it if necessary.
    """
    try:
        cont = _find_container_by_prompt(prompt_text)
        if not cont:
            print(f">*Ubiq*<Cannot find container for prompt '{prompt_text}'")
            return False

        btn = cont.find_element(By.CSS_SELECTOR, "button.muteButton")

        for attempt in range(max_retry):
            cur_state = _is_button_pressed(btn)
            if cur_state == target_mute:
                print(f">*Ubiq*<'{prompt_text}' is already mute={target_mute} (Attempt {attempt})")
                return True

            # If state is not the target, click and wait.
            driver.execute_script("arguments[0].click();", btn)
            time.sleep(wait_sec)

        final_state = _is_button_pressed(btn)
        print(f">*Ubiq*<cannot '{prompt_text}' set mute={target_mute}，final state={final_state}")
        return False

    except Exception as e:
        print(f">*Ubiq*<Error clicking muteButton: {e}")
        return False



def listen_from_node():
    """
    Listens for JSON commands from stdin (from Node.js) and adds actions to queues.
    """
    for raw in sys.stdin:
        print(f"[DEBUG] raw repr: {raw!r}")
        line = raw.lstrip('\ufeff').strip()
        if not line:
            continue
        try:
            msg = json.loads(line)
            mtype = msg.get("type")

            # Handle "DeletePrompt" command.
            if mtype == "DeletePrompt":
                keyword = msg.get("prompt", "").strip()
                if keyword:
                    with delete_lock:
                        delete_queue.append(keyword)
                    print(f">*Ubiq*<Queued delete for prompt: {keyword}")
                    debug(f">*Ubiq*<Queued delete for prompt: {keyword}")

            # Handle "SetPromptVolume" command.
            elif mtype == "SetPromptVolume":
                prompt  = msg.get("prompt", "").strip()
                volume  = msg.get("volume")          # Integer 0~100 
                if prompt and volume is not None:
                    with volume_lock:
                        volume_queue.append((prompt, volume))
                    debug(f">*Ubiq*<Queued volume {volume} for '{prompt}'")

            # Handle "Mute" command for a specific prompt.
            elif mtype == "Mute":
                prompt = msg.get("prompt", "").strip()
                target = bool(msg.get("mute"))  # true=mute
                if prompt:
                    ensure_mute_state(prompt, target)

            # Handle global settings like density, brightness, chaos.
            elif mtype in ("density", "brightness", "chaos"):
                level = (msg.get("level") or
                        msg.get("density") or  
                        "").strip().lower()     # "auto" / "low" / "high"

                if not level:
                    print(f">*Ubiq*< Missing level for {mtype}")
                    continue

                if mtype == "density":
                    ensure_density(level)
                elif mtype == "brightness":
                    ensure_brightness(level)
                else:                            # SetChaos
                    ensure_chaos(level)
                debug(f">*Ubiq*<Executed {mtype} -> {level}")
            
            # Handle BPM change.
            elif mtype == "bpm":
                ensure_bpm(msg.get("value", "auto"))
            
            # Handle track muting (drums, bass, other).
            elif mtype == "TrackMute":
                track = msg.get("track", "").strip().lower()   # drums / bass / other
                target = bool(msg.get("mute"))                 # True=mute
                if track in ("drums", "bass", "other"):
                    ensure_track_mute(track, target)
            
            # Handle musical key change.
            elif mtype == "key":
                val = (msg.get("value") or "").strip()
                if val:
                    ensure_key(val)

        except Exception as e:
            print(f"[From Node] JSON error: {e}")

            
def get_prompt_text(container):
    """Helper to extract prompt text from a container element."""
    try:
        return container.find_element(By.CSS_SELECTOR, "div.kWfOUR").text.strip()
    except Exception:
        return container.find_element(By.XPATH, ".//div[not(*) and normalize-space()]").text.strip()

def norm_vol(v):
    """Helper to normalize a volume value to a 0-100 integer."""
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
    """Finds a prompt's container element by its text content."""
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

def print_all_prompt_volumes():
    """
    Executes JS to get the text and volume values of all prompts and prints them.
    """
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
    Sets a prompt's volume by injecting JavaScript to call the underlying React handler.
    This is more reliable than simulating a drag-and-drop.
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
      // Find the trackContainer for the given prompt name.
      const containers = [...document.querySelectorAll('div.trackContainer')];
      const cont = containers.find(c=>{
        const t = c.querySelector('div.kWfOUR') || c.querySelector("div:not(:has(*))");
        return t && t.textContent.trim() === name.trim();
      });
      if(!cont) return 'notfound';

      // Find the slider element.
      const sliderEl = cont.querySelector('span[role="slider"][aria-valuenow]');
      if(!sliderEl) return 'no slider';

      // Read min/max values and calculate the real value from the percentage.
      const vmin = parseFloat(sliderEl.getAttribute('aria-valuemin') || '0');
      const vmax = parseFloat(sliderEl.getAttribute('aria-valuemax')  || '100');
      const realVal = vmin + (vmax - vmin) * (pct/100);

      // Find the React Fiber instance to access its props.
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

      // Call the onValueChange handler with the new value.
      handler([realVal]);

      return {match: name, min:vmin, max:vmax, setPercent:pct, setReal:realVal};
    })(arguments[0], arguments[1]);
    """
    result = driver.execute_script(js, prompt, pct)
    print(f">*Ubiq*<set_prompt_volume_percent('{prompt}', {pct}) -> {result}")
    debug(f">*Ubiq*<set_prompt_volume_percent('{prompt}', {pct}) -> {result}")
    return result

# ---------- Generic UI control helpers ---------- #
def _find_button(prefix: str, level: str):
    """
    Finds a button using multiple strategies: by ID, by data-motion attribute, or by relative position.
    """
    # find ID
    try:
        return driver.find_element(By.ID, f"{prefix}_{level}")
    except Exception:
        pass

    # 2) data-motion attribute（Google MusicFX has data-motion-pop-id）
    try:
        return driver.find_element(
            By.CSS_SELECTOR,
            f'button[data-motion-pop-id*="{prefix}"][data-motion-pop-id*="{level}"]'
        )
    except Exception:
        pass

    # by relative position
    try:
        label_texts = {
            "density": ("density", "密度"),
            "brightness": ("brightness", "亮度"),
            "chaos": ("chaos", "混乱", "随机")   # different languages
        }
        label_words = label_texts.get(prefix, ())
        for word in label_words:
            lab = driver.find_element(By.XPATH, f"//label[contains(text(), '{word}')]")
            btns = lab.find_elements(By.XPATH, ".//preceding::button")[-3:]
            idx  = {"auto":0, "low":1, "high":2}[level]
            return btns[idx]
    except Exception:
        pass
    return None


def _is_selected(button):
    """Helper to check if a button is in a selected/active state."""
    aria = button.get_attribute("aria-pressed")
    if aria:
        return aria.lower() == "true"
    cls = (button.get_attribute("class") or "").lower()
    return "active" in cls or "selected" in cls or "toggled" in cls


def _ensure(prefix: str, level: str, retry=3, wait=0.15):
    """Generic function to ensure a setting (like density) is at the desired level."""
    level = level.lower().strip()
    if level not in ("auto", "low", "high"):
        debug(f">*Ubiq*<Invalid level '{level}' for {prefix}")
        return False

    btn = _find_button(prefix, level)
    if not btn:
        debug(f">*Ubiq*<Button not found: {prefix}_{level}")
        return False

    for _ in range(retry):
        if _is_selected(btn):
            debug(f">*Ubiq*<{prefix} is already {level}")
            return True
        driver.execute_script("arguments[0].click();", btn)
        time.sleep(wait)
    debug(f">*Ubiq*<Failed to switch {prefix} to {level}")
    return False

def ensure_density(level: str, max_retry=3, wait_sec=0.15):
    return _ensure("density", level, max_retry, wait_sec)

def ensure_brightness(level: str, max_retry=3, wait_sec=0.15):
    return _ensure("brightness", level, max_retry, wait_sec)

def ensure_chaos(level: str, max_retry=3, wait_sec=0.15):
    return _ensure("chaos", level, max_retry, wait_sec)

# 60–180 BPM constrains
BPM_MIN = 60
BPM_MAX = 180


def _find_bpm_button():
    """Finds the main BPM button on the page."""
    XPATH_BTN = (
        "//button[@aria-haspopup='dialog' and .//small[normalize-space()='BPM']]"
        " | //button[contains(.,'BPM')]"
    )
    return driver.find_element(By.XPATH, XPATH_BTN)

def _wait_bpm_panel(btn, timeout=5):
    """Waits for the BPM control panel to become visible after clicking the button."""
    pid = btn.get_attribute("aria-controls")
    if not pid:
        raise TimeoutException("BPM button missing aria-controls")
    panel = WebDriverWait(driver, timeout).until(
        EC.visibility_of_element_located((By.ID, pid))
    )
    track = panel.find_element(By.CSS_SELECTOR,
                               "span[data-orientation='horizontal']")
    return panel, track


def ensure_bpm(value):
    """Opens the BPM panel and sets the BPM to the specified value."""
    debug(f"[BPM] enter ensure_bpm({value})")
    # open panel
    try:
        top_btn = _find_bpm_button()
        driver.execute_script("arguments[0].click();", top_btn)
    except Exception as e:
        debug(f"[BPM]  click top-btn: {e}")
        return False

    try:
        panel, track = _wait_bpm_panel(top_btn)
    except TimeoutException:
        debug("[BPM]  panel timeout")
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

    # Use JS to set the slider's value directly.
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

    # click apply
    try:
        panel.find_element(By.XPATH,
            ".//button[normalize-space()='应用' or normalize-space()='Apply']"
        ).click()
    except Exception:
        pass
    return True


def _find_key_button(timeout=3):
    """Finds the main musical key button using several strategies."""
    XPATHS = [
        "//button[@aria-haspopup='dialog' and contains(normalize-space(.),'键')]",
        "//button[@aria-haspopup='dialog' and contains(normalize-space(.),'Key')]",
        "//button[@aria-haspopup='dialog' and (.//*[contains(normalize-space(),'maj') or contains(normalize-space(),'min')]"
        " or contains(normalize-space(.),'maj') or contains(normalize-space(.),'min'))]",
    ]
    for xp in XPATHS:
        try:
            return WebDriverWait(driver, timeout).until(
                EC.element_to_be_clickable((By.XPATH, xp))
            )
        except Exception:
            pass

    # Another dialog button on the same line (same parent) as BPM
    try:
        bpm = WebDriverWait(driver, 3).until(
            EC.presence_of_element_located(
                (By.XPATH, "//button[@aria-haspopup='dialog' and .//small[normalize-space()='BPM']]")
            )
        )
        # Go to the nearest container and find the brother button
        row = bpm.find_element(By.XPATH, "./..")
        siblings = [b for b in row.find_elements(By.XPATH, ".//button[@aria-haspopup='dialog']") if b.is_displayed()]
        if len(siblings) >= 2:
            # Filter out BPM itself and take another
            for b in siblings:
                if "BPM" not in (b.text or ""):
                    return b
    except Exception:
        pass

    # Backup: The first dialog button after BPM
    try:
        sib = driver.find_element(By.XPATH, "//button[@aria-haspopup='dialog' and .//small[normalize-space()='BPM']]/following::button[@aria-haspopup='dialog'][1]")
        if sib.is_displayed():
            return sib
    except Exception:
        pass

    return None

def _wait_key_panel(btn, timeout=5):
    """Waits for the key selection panel to become visible."""
    pid = btn.get_attribute("aria-controls")
    if pid:
        return WebDriverWait(driver, timeout).until(
            EC.visibility_of_element_located((By.ID, pid))
        )

    # Fallback: Find a visible listbox/dialog (commonly used by Radix)
    return WebDriverWait(driver, timeout).until(
        EC.visibility_of_element_located((
            By.XPATH,
            "//*[(@role='listbox' or @role='dialog' or starts-with(@id,'radix-')) and not(@aria-hidden='true')]"
        ))
    )


def ensure_key(value: str):
    """Opens the key panel and selects the specified key."""
    label = (value or "").strip()
    debug(f"[KEY] enter ensure_key({label})")
    if not label:
        return False

    # Open the top Key button
    try:
        key_btn = _find_key_button(timeout=5)
        driver.execute_script("arguments[0].scrollIntoView({block:'center'});", key_btn)
        driver.execute_script("arguments[0].click();", key_btn)
    except Exception as e:
        debug(f"[KEY] click key button failed: {e}")
        return False

    # Wait for the outer dialog to be visible
    try:
        panel = _wait_key_panel(key_btn, timeout=6)
        if not panel:
            debug("[KEY] panel not found after click")
            return False
    except TimeoutException:
        debug("[KEY] panel timeout")
        return False

    # "Auto/Automatic" directly goes to the reset button
    if label.lower() in ("auto", "自动"):
        try:
            try:
                reset_btn = panel.find_element(By.XPATH, ".//button[normalize-space()='重置' or normalize-space()='Reset']")
                driver.execute_script("arguments[0].click();", reset_btn)
            finally:
                apply_btn = panel.find_element(By.XPATH, ".//button[normalize-space()='应用' or normalize-space()='Apply']")
                driver.execute_script("arguments[0].click();", apply_btn)
            return True
        except Exception as e:
            debug(f"[KEY] reset->apply failed: {e}")
            return False

    # Find the combobox in the dialog and click it to render the actual listbox
    try:
        combo = panel.find_element(By.XPATH, ".//button[@role='combobox' and @aria-controls]")
        lb_id = combo.get_attribute("aria-controls")
        driver.execute_script("arguments[0].click();", combo)

        # Wait for the listbox to appear
        listbox = WebDriverWait(driver, 6).until(
            EC.visibility_of_element_located((By.ID, lb_id))
        )
    except Exception as e:
        debug(f"[KEY] open combobox/listbox failed: {e}")
        return False

    # Select the target item in the listbox
    normA = label
    normB = label.replace(" / ", "/")
    try:
        try:
            opt = listbox.find_element(
                By.XPATH,
                f".//*[(normalize-space(text())='{normA}') or (normalize-space(text())='{normB}')]"
            )
        except Exception:
            opt = listbox.find_element(
                By.XPATH,
                f".//*[contains(normalize-space(.), '{normA}') or contains(normalize-space(.), '{normB}')]"
            )
        driver.execute_script("arguments[0].scrollIntoView({block:'center'});", opt)
        driver.execute_script("arguments[0].click();", opt)
    except Exception:
        # JS fallback (limited to the current listbox)
        js_pick = r"""
          const root = arguments[0];
          const label = String(arguments[1]||'').trim();
          const norm  = s => String(s||'').replace(/\s+/g,' ').replace(' / ','/').trim();
          const isVis = el => !!(el && el.getClientRects().length);

          const nodes = [...root.querySelectorAll('[role=option],button,div,span')].filter(isVis);
          let el = nodes.find(e => norm(e.textContent) === norm(label));
          if(!el) el = nodes.find(e => norm(e.textContent).includes(norm(label)));
          if(!el) return 'no option';
          el.scrollIntoView({block:'center'}); el.click();
          return 'ok';
        """
        res = driver.execute_script(js_pick, listbox, label)
        debug(f"[KEY] js-pick in listbox '{label}' -> {res}")
        if res != "ok":
            return False

    # Click "Apply" (if available)
    try:
        apply_btn = panel.find_element(By.XPATH, ".//button[normalize-space()='应用' or normalize-space()='Apply']")
        driver.execute_script("arguments[0].click();", apply_btn)
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
    """Finds the mute button for a specific track (drums, bass, other)."""
    track = track.lower().strip()
    btn_id = _TRACK_ID_MAP.get(track)
    if not btn_id:
        return None
    try:
        return driver.find_element(By.ID, btn_id)
    except Exception:
        return None

def _is_btn_muted(btn) -> bool|None:
    """Checks if a track button is currently in the muted state."""
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
    """Ensures a track is in the target mute state."""
    btn = _find_track_button(track)
    if not btn:
        debug(f">*Ubiq*<Track button not found: {track}")
        return False

    for _ in range(retry):
        cur = _is_btn_muted(btn)
        if cur == target_mute:
            debug(f">*Ubiq*<{track} is already mute={target_mute}")
            return True
        driver.execute_script("arguments[0].click();", btn)
        time.sleep(wait)

    debug(f">*Ubiq*<Failed to set {track} to mute={target_mute}")
    return False

# ---------- Page Clear Helpers ---------- #
_cleared_once = False

_cleared_once = False

_cleared_once = False

def _count_prompts_js():
    return """
      return (function(){
        // JS to count the number of prompt containers on the page.
        const nodes = document.querySelectorAll('div.trackContainer');
        return nodes ? nodes.length : 0;
      })();
    """

def _install_auto_clear_observer():
    """Injects a MutationObserver to automatically click the 'Clear All' button."""
    js = r"""
    (function(){
      if(window.__autoClearInstalled) return 'installed';
      window.__autoClearInstalled = true;
      window.__autoClearing = true;

      const tryClick = ()=>{
        if(!window.__autoClearing) return false; 
        const btn = document.getElementById('clearAllPrompts') 
                 || document.querySelector('button#clearAllPrompts, button[id*="clear"][id*="prompt"]');
        if(btn){
          try{
            const ev = (n)=>btn.dispatchEvent(new MouseEvent(n,{bubbles:true,cancelable:true,view:window}));
            ev('pointerdown'); ev('mousedown'); ev('click'); ev('pointerup'); ev('mouseup');
          }catch(e){ btn.click(); }
          return true;
        }
        return false;
      };

      // Try several times immediately (only triggered when in the enabled state)
      setTimeout(tryClick,   0);
      setTimeout(tryClick, 300);
      setTimeout(tryClick, 800);
      setTimeout(tryClick,1500);

      // Listen for DOM changes; only act when the switch is on
      const obs = new MutationObserver(()=>tryClick());
      obs.observe(document.documentElement,{childList:true,subtree:true});
      window.__autoClearObs = obs;

      return 'ok';
    })();
    """
    res = driver.execute_script(js)
    debug(f"[ClearAll] inject observer -> {res}")

def wait_page_ready_and_clear(timeout=40, verify_timeout=8.0):
    """Robustly clears all prompts from the page on startup."""
    global _cleared_once, driver
    if _cleared_once:
        return

    t0 = time.time()
    debug("[ClearAll] >>> start")

    try:
        # DOM ready
        WebDriverWait(driver, timeout).until(
            lambda d: d.execute_script("return document.readyState") == "complete"
        )
        debug("[ClearAll] document.readyState=complete")

        # clean storage
        try:
            driver.execute_script("try{ localStorage.clear(); sessionStorage.clear(); }catch(e){}")
            debug("[ClearAll] storage cleared")
        except Exception as e:
            debug(f"[ClearAll] storage clear error: {e}")

        # Inject automatic emptying observer
        _install_auto_clear_observer()

        # Active click once + verification cycle
        def _try_click_once():
            js_click = r"""
            const btn = document.getElementById('clearAllPrompts')
                   || document.querySelector('button#clearAllPrompts, button[id*="clear"][id*="prompt"]');
            if(!btn) return 'no-btn';
            try{
              const ev = (n)=>btn.dispatchEvent(new MouseEvent(n,{bubbles:true,cancelable:true,view:window}));
              ev('pointerdown'); ev('mousedown'); ev('click'); ev('pointerup'); ev('mouseup');
            }catch(e){ btn.click(); }
            return 'clicked';
            """
            return driver.execute_script(js_click)

        # try click
        res = _try_click_once()
        debug(f"[ClearAll] first click -> {res}")

        # Verification: Wait for trackContainer to become 0 and remain 0 for a period of time
        end_at = time.time() + verify_timeout
        last_n = None
        stable_zero_time = None

        while time.time() < end_at:
            n = driver.execute_script(_count_prompts_js())
            if n != last_n:
                debug(f"[ClearAll] prompts={n}")
                last_n = n

            # In the verification loop, when it is confirmed to be stable at 0:
            if n == 0:
                if stable_zero_time is None:
                    stable_zero_time = time.time()
                if time.time() - stable_zero_time >= 0.6:
                    debug("[ClearAll] confirmed 0 and stable")
                    # Clear once and then turn off (disconnect and turn off the switch)
                    try:
                        driver.execute_script("""
                        if(window.__autoClearObs){ window.__autoClearObs.disconnect(); }
                        window.__autoClearing = false;
                        """)
                        debug("[ClearAll] observer disconnected and disabled")
                    except Exception as e:
                        debug(f"[ClearAll] disable observer error: {e}")
                    _cleared_once = True
                    return
            else:
                stable_zero_time = None
                # try click again
                _try_click_once()
            time.sleep(0.2)

        debug("[ClearAll] verify timeout, still not stable 0")

    except Exception as e:
        debug(f"[ClearAll] error: {e}")
    finally:
        debug(f"[ClearAll] <<< done in {time.time()-t0:.2f}s")


if __name__ == "__main__":

    threading.Thread(target=listen_from_node, daemon=True).start()

    parser = argparse.ArgumentParser(description="Text-to-Sound")
    parser.add_argument("--prompt_postfix", type=str, default="", help="Postfix to add to the prompt.")
    args = parser.parse_args()

    chrome_options = Options()
    chrome_options.add_experimental_option("debuggerAddress", "127.0.0.1:9222")
    chrome_options.add_argument("--mute-audio")

    # service = Service("D:/Applications/chromedriver-win64/chromedriver.exe")
    # chromedriver_path = Path("D:/Google Download/chromedriver-win64/chromedriver-win64/chromedriver.exe")
    chromedriver_path = Path("D:/chromedriver-win64/chromedriver.exe")
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

    # wait_page_ready_and_clear()

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
