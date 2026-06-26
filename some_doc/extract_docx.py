import zipfile
import xml.etree.ElementTree as ET
import os

docx_path = r"d:\Desktop\FU\do an\Capstone Project\Report 3.2_FDS.docx"
txt_output_path = r"d:\Desktop\FU\do an\Capstone Project\Report_FDS_extracted.txt"

if not os.path.exists(docx_path):
    print(f"File not found: {docx_path}")
    exit()

try:
    with zipfile.ZipFile(docx_path) as docx:
        xml_content = docx.read('word/document.xml')
        root = ET.fromstring(xml_content)
        
        # XML namespace thông thường của word
        ns = {'w': 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'}
        
        paragraphs = []
        # Duyệt qua các paragraph (w:p)
        for paragraph in root.iter('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}p'):
            # Lấy toàn bộ thẻ w:t trong paragraph này
            texts = [node.text for node in paragraph.iter('{http://schemas.openxmlformats.org/wordprocessingml/2006/main}t') if node.text]
            if texts:
                paragraphs.append("".join(texts))
            else:
                paragraphs.append("")
                
        full_text = "\n".join(paragraphs)
        with open(txt_output_path, "w", encoding="utf-8") as f:
            f.write(full_text)
        print(f"Successfully extracted text to {txt_output_path}")
except Exception as e:
    print(f"Error: {e}")
