"""Shared portrait reduction calibrated against the supplied original/export pair."""
from PIL import Image


def render_portrait(image, size, position, resampling='box', canvas_size=(288, 212)):
    image = image.convert('RGBA')
    bbox = image.getchannel('A').point(lambda a: 255 if a >= 128 else 0).getbbox()
    if bbox is None:
        raise ValueError('Empty source portrait')
    sw, sh = bbox[2]-bbox[0], bbox[3]-bbox[1]
    if size[0] != round(sw*size[1]/sh):
        raise ValueError('Portrait export must preserve the master aspect ratio')
    image = image.crop(bbox)
    if resampling == 'box':
        image = image.convert('RGBa').resize(size, Image.Resampling.BOX).convert('RGBA')
    elif resampling == 'nearest':
        image = image.resize(size, Image.Resampling.NEAREST)
    else:
        raise ValueError('Unknown portrait resampling: '+resampling)
    image.putdata([(*px[:3],255) if px[3]>=128 else (0,0,0,0)
                   for px in image.get_flattened_data()])
    canvas = Image.new('RGBA',canvas_size,(0,0,0,0))
    canvas.paste(image,position)
    return canvas,bbox
