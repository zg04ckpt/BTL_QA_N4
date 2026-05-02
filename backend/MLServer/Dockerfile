FROM python:3.9-slim
WORKDIR /app
COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt
COPY . .
# Tạo lại model + TF-IDF trong image (cùng phiên bản scikit-learn) — tránh lỗi "idf vector is not fitted" do .pkl cũ/hỏng
RUN python retrain_optimized.py
EXPOSE 2002
CMD ["python", "app.py"]
