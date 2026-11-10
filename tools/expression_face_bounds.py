"""Keep expression layers inside manually reviewed native lower-face bounds.

Portrait alpha protects the character's outer silhouette, but cannot distinguish
the jaw from the opaque neck underneath. These bounds sit inside each neutral
jaw and are independent of the generated face's jaw/skin colours.
"""
import numpy as np
from PIL import Image, ImageDraw, ImageFilter


# Native 288x212 coordinates, reviewed against each actual neutral portrait.
# Upper-face pixels are untouched; the lower edge leaves the jaw and neck from
# neutral. Do not infer this edge from a generated portrait or its iris area.
LOWER_FACE_EDGES = {
    'eleanor1': [(116, 124), (122, 125), (128, 128), (134, 131),
                 (140, 134), (143, 134), (147, 133), (153, 129),
                 (158, 125), (164, 124)],
    'eleanor_scav': [(112, 124), (118, 126), (124, 130), (130, 133),
                    (135, 135), (140, 134), (146, 130), (152, 126), (158, 124)],
    'eleanor_maid': [(116, 124), (122, 126), (128, 130), (134, 133),
                    (140, 136), (144, 136), (150, 133), (156, 129),
                    (161, 125), (167, 124)],
    'lydia_daily': [(116, 124), (122, 127), (128, 131), (134, 134),
                   (140, 135), (144, 134), (150, 130), (156, 125), (162, 124)],
    'lydia_date': [(116, 124), (122, 127), (128, 131), (134, 134),
                  (140, 135), (144, 134), (150, 130), (156, 125), (162, 124)],
}


def jaw_guard(size, family):
    if size != (288, 212):
        raise ValueError('Review facial bounds for a different native canvas')
    edge = LOWER_FACE_EDGES[family]
    mask = Image.new('L', size)
    ImageDraw.Draw(mask).polygon([(0, 0), (287, 0), (287, 124),
                                 *reversed(edge), (0, 124)], fill=255)
    inside = np.asarray(mask).astype(float) / 255
    # Feather inward only. A blur without this hard clip would reintroduce skin
    # onto the very jaw/neck pixels the bound is meant to protect.
    guard = inside * (np.asarray(mask.filter(ImageFilter.GaussianBlur(.55))).astype(float) / 255)
    guard[:124] = 1
    return guard
