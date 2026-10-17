#!/usr/bin/env python3
import json
import os
import sys
from pathlib import Path

import requests

NEW_ENTRIES_FILE = Path("new_changelog_entries.json")
DISCORD_WEBHOOK_URL = os.environ.get("DISCORD_WEBHOOK_URL")

EMBED_COLOR = 0xFF00FF  # embed accent color (fuchsia)

# Emoji per change type. Chosen to match SS14's visual style.
TYPES_TO_EMOJI = {
    "Add": "✨",
    "Fix": "🐛",
    "Tweak": "⚒️",
    "Remove": "❌",
}

# Discord embed limits.
MAX_IMAGE_URL_LENGTH = 2048
MAX_FIELD_VALUE_LENGTH = 1024

def group_by_pr(entries: list[dict]) -> dict[str, list[dict]]:
    """Group entries by PR URL while preserving insertion order."""
    grouped: dict[str, list[dict]] = {}
    for entry in entries:
        url = entry.get("url")
        if not url:
            continue
        grouped.setdefault(url, []).append(entry)
    return grouped


def build_embed(pr_url: str, entries: list[dict]) -> dict:
    """
    Build a single embed for one PR.

    The first image (if any) goes into the embed's `image` field.
    Videos cannot be played inside a rich embed, so they are added as
    clickable, numbered links ("Демонстрация N") in a `fields` entry.
    Author always comes from the PR's GitHub user.
    """
    author = entries[0].get("author", "unknown")

    lines: list[str] = []
    for entry in entries:
        for change in entry.get("changes", []):
            emoji = TYPES_TO_EMOJI.get(change.get("type"), "•")
            lines.append(f"{emoji} {change.get('message', '')}")

    description = "\n".join(lines)
    # Discord hard-limits embed descriptions to 4096 characters.
    if len(description) > 4096:
        description = description[:4093] + "..."

    embed: dict = {
        "title": f"Изменение от {author}",
        "url": pr_url,
        "description": description,
        "color": EMBED_COLOR,
    }

    # Collect media from all entries for this PR (usually just one).
    images: list[str] = []
    videos: list[str] = []
    for entry in entries:
        media = entry.get("media") or {}
        images.extend(media.get("images", []))
        videos.extend(media.get("videos", []))

    # Only the first valid image is shown inline; the rest are dropped.
    for img in images:
        if img and len(img) <= MAX_IMAGE_URL_LENGTH:
            embed["image"] = {"url": img}
            break

    # Videos (GitHub uploads + YouTube): numbered clickable links
    # in a single "Медиа" field.
    video_links = []
    for i, vid in enumerate(videos, start=1):
        if len(vid) > MAX_IMAGE_URL_LENGTH:
            continue
        video_links.append(f"[Демонстрация {i}]({vid})")

    if video_links:
        value = "\n".join(video_links)
        if len(value) > MAX_FIELD_VALUE_LENGTH:
            value = value[: MAX_FIELD_VALUE_LENGTH - 3] + "..."
        embed.setdefault("fields", []).append(
            {"name": "Медиа", "value": value, "inline": False}
        )

    return embed


def send_embed(embed: dict) -> None:
    """Post a single embed to the configured Discord webhook."""
    payload = {
        "embeds": [embed],
        # Never ping anyone, even if the message text contains @mentions.
        "allowed_mentions": {"parse": []},
    }
    resp = requests.post(DISCORD_WEBHOOK_URL, json=payload, timeout=15)
    resp.raise_for_status()
    print(f"Sent embed for {embed['url']}")


def main() -> None:
    if not DISCORD_WEBHOOK_URL:
        print("DISCORD_WEBHOOK_URL is not set; skipping Discord publish.")
        return

    if not NEW_ENTRIES_FILE.exists():
        print("No new entries file found; nothing to publish.")
        return

    entries = json.loads(NEW_ENTRIES_FILE.read_text(encoding="utf-8"))
    if not entries:
        print("No new entries to publish.")
        return

    grouped = group_by_pr(entries)
    for pr_url, pr_entries in grouped.items():
        send_embed(build_embed(pr_url, pr_entries))


if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        print(f"send_discord_embeds.py failed: {e}", file=sys.stderr)
        sys.exit(1)
