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


# Native coordinates for the brow on the viewer's left. Each patch includes the
# neutral brow and the space the aligned brow moves through, inside the bangs.
LEFT_BROW_PATCHES = {
    'eleanor_scav': (('angry', 'determined'),
                     [(113, 103), (120, 102), (122, 104), (123, 107), (125, 110),
                      (126, 112), (120, 113), (112, 112), (111, 109)]),
    'eleanor1': (('angry', 'serious'),
                 [(120, 105), (127, 103), (129, 103), (130, 105), (132, 108),
                  (134, 112), (133, 114), (120, 113), (119, 111), (119, 107)]),
    'eleanor_maid': (('angry', 'serious', 'tsun'),
                     [(120, 105), (126, 103), (129, 103), (130, 105), (132, 109),
                      (135, 112), (132, 114), (119, 112), (117, 110), (119, 107)]),
    'lydia_daily': (('angry', 'determined', 'sad'),
                    [(124, 104), (126, 103), (128, 103), (130, 105), (131, 108),
                     (130, 110), (124, 111), (122, 110), (123, 107)]),
    'lydia_date': (('angry', 'determined', 'sad'),
                   [(125, 104), (127, 103), (129, 103), (131, 105), (132, 108),
                    (131, 110), (124, 111), (123, 110), (124, 107)]),
}


def include_left_brow(weight, family, expression, origin=(0, 0)):
    profile = LEFT_BROW_PATCHES.get(family)
    if profile is None or expression not in profile[0]:
        return weight

    x0, y0 = origin
    stencil = Image.new('L', (weight.shape[1], weight.shape[0]))
    ImageDraw.Draw(stencil).polygon([(x - x0, y - y0) for x, y in profile[1]], fill=255)
    feather = np.asarray(stencil.filter(ImageFilter.GaussianBlur(.4))) / 255
    return np.maximum(weight, (np.asarray(stencil) > 0) * feather)


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
