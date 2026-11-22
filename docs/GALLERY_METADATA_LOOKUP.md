# 画廊图片提示词与参数快速查询

资源名：image-xxxxxxxx.png

查询命令：
python tools/gallery_lookup.py image-xxxxxxxx.png

JSON 输出：
python tools/gallery_lookup.py image-xxxxxxxx.png --json

数据位置：
C:\Users\Administrator\AppData\Roaming\@deepseek-ai\dsh-desktop\IndexedDB\dsh-app_app_0.indexeddb.leveldb

数据库：dsh_image_gen_db
表：gallery_history
记录键：id = sha256:...
资源名对应字段：savedTo
提示词字段：prompt
参数字段：provider、channel、model、output、savedTo、sessionId、createdAt
