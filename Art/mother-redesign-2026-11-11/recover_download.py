"""Recover the already generated image with small verified HTTP GET ranges."""
from concurrent.futures import ThreadPoolExecutor, as_completed
from pathlib import Path
import argparse
import hashlib
import json
import time
import urllib.request

from PIL import Image

HERE = Path(__file__).resolve().parent
URL = 'https://oss.codexzh.com/cz-img/images/2026/11/11/0811846aeb8243d88d817df3a4979892.png'
TOTAL = 1843946
CHUNK = 32768
PARTS = HERE / 'masters/download_parts'
PARTS.mkdir(exist_ok=True)


def get_part(start):
    end = min(start + CHUNK, TOTAL) - 1
    target = PARTS / f'{start:08d}.part'
    if target.exists() and target.stat().st_size == end - start + 1:
        return target
    for attempt in range(4):
        try:
            request = urllib.request.Request(URL, headers={
                'Range': f'bytes={start}-{end}',
                'User-Agent': 'Mozilla/5.0', 'Accept-Encoding': 'identity',
            })
            with urllib.request.urlopen(request, timeout=18) as response:
                assert response.status == 206
                assert response.headers['Content-Range'] == f'bytes {start}-{end}/{TOTAL}'
                data = response.read()
            assert len(data) == end - start + 1
            target.write_bytes(data)
            return target
        except Exception:
            if attempt == 3:
                raise
            time.sleep(1)


def main():
    global URL, TOTAL, PARTS
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--url', default=URL)
    parser.add_argument('--total', type=int, default=TOTAL)
    parser.add_argument('--output', default='masters/mother_beauty_master_v2.png')
    args = parser.parse_args()
    URL, TOTAL = args.url, args.total
    assert URL.startswith('https://oss.codexzh.com/cz-img/images/')
    assert 0 < TOTAL < 20000000
    target = (HERE / args.output).resolve()
    assert target.is_relative_to(HERE / 'masters')
    PARTS = HERE / 'masters' / ('download_parts_' + target.stem)
    PARTS.mkdir(exist_ok=True)
    starts = list(range(0, TOTAL, CHUNK))
    with ThreadPoolExecutor(max_workers=8) as executor:
        jobs = [executor.submit(get_part, start) for start in starts]
        for count, job in enumerate(as_completed(jobs), 1):
            job.result()
            if count % 8 == 0 or count == len(jobs):
                print(f'Downloaded {count}/{len(jobs)} verified chunks', flush=True)
    data = b''.join((PARTS / f'{start:08d}.part').read_bytes() for start in starts)
    assert len(data) == TOTAL
    temporary = target.with_suffix('.recovered')
    temporary.write_bytes(data)
    with Image.open(temporary) as image:
        image.verify()
    temporary.replace(target)
    report = {'url': URL, 'total_bytes': TOTAL, 'method': 'HTTP GET byte ranges; no new image-generation POST',
              'sha256': hashlib.sha256(data).hexdigest(), 'output': target.relative_to(HERE).as_posix()}
    (HERE / (target.stem + '_download_recovery.json')).write_text(json.dumps(report, indent=2)+'\n', encoding='utf-8')
    print(f'Saved complete verified image: {target}', flush=True)


if __name__ == '__main__':
    main()
