import tkinter as tk
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler
import os
import re

# File to be monitored
FILE_PATH = os.path.abspath("../../apps/continuous_music/data/gpt.txt")


class FileWatcherHandler(FileSystemEventHandler):
    def __init__(self, app):
        self.app = app

    def on_modified(self, event):
        print(f">*Ubiq*<Detected change in: {event.src_path}")
        if event.src_path == FILE_PATH:
            self.update_content()

    def update_content(self):
        try:
            with open(FILE_PATH, 'r', encoding='utf-8') as file:
                content = file.read()

            # Extract sentences within square brackets
            sentences = re.findall(r'\[(.*?)\]', content)
            self.app.update_sentences(sentences)
        except Exception as e:
            self.app.display_error(f"Error reading file: {e}")


class SentenceDisplayApp:
    def __init__(self, root):
        self.root = root
        self.root.title("File Watcher")

        # Scrollable container for sentence fields
        self.scrollable_frame = tk.Frame(root)
        self.scrollable_frame.pack(fill=tk.BOTH, expand=True)

        self.canvas = tk.Canvas(self.scrollable_frame)
        self.scrollbar = tk.Scrollbar(self.scrollable_frame, orient="vertical", command=self.canvas.yview)
        self.scrollable_container = tk.Frame(self.canvas)

        self.scrollable_container.bind(
            "<Configure>",
            lambda e: self.canvas.configure(scrollregion=self.canvas.bbox("all"))
        )

        self.canvas.create_window((0, 0), window=self.scrollable_container, anchor="nw")
        self.canvas.configure(yscrollcommand=self.scrollbar.set)

        self.canvas.pack(side=tk.LEFT, fill=tk.BOTH, expand=True)
        self.scrollbar.pack(side=tk.RIGHT, fill=tk.Y)

        self.sentence_fields = []  # Store references to sentence widgets

    def update_sentences(self, sentences):
        # Clear existing widgets
        for widget in self.scrollable_container.winfo_children():
            widget.destroy()

        self.sentence_fields = []

        # Add a row for each sentence
        for i, sentence in enumerate(sentences):
            frame = tk.Frame(self.scrollable_container)
            frame.pack(fill=tk.X, pady=5, padx=10)

            # Text field
            entry = tk.Entry(frame, font=("Arial", 12), state="normal", width=50)
            entry.pack(side=tk.LEFT, fill=tk.X, expand=True, padx=(0, 5))
            entry.insert(0, sentence)
            entry.configure(state="readonly")

            # Copy button
            button = tk.Button(frame, text="Copy", command=lambda s=sentence, e=entry: self.copy_to_clipboard(s, e))
            button.pack(side=tk.RIGHT)

            self.sentence_fields.append(entry)

    def copy_to_clipboard(self, sentence, entry):
        # Copy the sentence to clipboard
        self.root.clipboard_clear()
        self.root.clipboard_append(sentence)
        self.root.update()  # Required to ensure clipboard updates immediately

        # Highlight the copied entry
        for e in self.sentence_fields:
            e.config(readonlybackground="white")  # Reset all entries
        entry.config(readonlybackground="lightyellow")  # Highlight the copied entry

    def display_error(self, message):
        # Clear existing widgets and display the error message
        for widget in self.scrollable_container.winfo_children():
            widget.destroy()

        label = tk.Label(self.scrollable_container, text=message, fg="red", font=("Arial", 12))
        label.pack(pady=10)


def start_gui():
    root = tk.Tk()
    app = SentenceDisplayApp(root)

    # Set up the file watcher
    handler = FileWatcherHandler(app)
    observer = Observer()
    observer.schedule(handler, path=os.path.dirname(FILE_PATH), recursive=False)
    observer.start()

    # Update the content once on start
    handler.update_content()

    # Stop observer when the application is closed
    def on_closing():
        observer.stop()
        observer.join()
        root.destroy()

    root.protocol("WM_DELETE_WINDOW", on_closing)
    root.mainloop()


if __name__ == "__main__":
    start_gui()
