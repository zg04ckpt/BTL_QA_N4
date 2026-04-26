import pandas as pd
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.svm import LinearSVC
from sklearn.model_selection import train_test_split
from sklearn.metrics import classification_report, accuracy_score
import pickle
import re

def clean_text(text):
    # Chuẩn hóa cơ bản: chuyển chữ thường, xóa khoảng trắng thừa
    text = str(text).lower()
    text = re.sub(r'\s+', ' ', text).strip()
    return text

print("Reading and cleaning dataset...")
df = pd.read_csv('training/dataset.txt', delimiter=',', header=0)
df['comment_text'] = df['comment_text'].apply(clean_text)
X = df['comment_text']
y = df['type']

X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.1, random_state=59, stratify=y)

print("Reading stopwords...")
with open('vietnamese-stopwords.txt', 'r', encoding='utf-8') as f:
    vietnamese_stopwords = [line.strip().lower() for line in f.readlines()]

# TỐI ƯU HÓA Ở ĐÂY: Thêm ngram_range=(1, 2) hoặc (1, 3)
# Nó sẽ giúp bắt được các cụm từ ghép tiếng Việt thay vì từng chữ rời rạc
print("Training TF-IDF with N-Grams...")
tfidf = TfidfVectorizer(
    stop_words=vietnamese_stopwords, 
    ngram_range=(1, 2), # Bắt cả cụm 2 từ (VD: "dở tệ", "rác rưởi", "không ngon")
    max_features=10000  # Giới hạn số lượng đặc trưng để tiết kiệm RAM
)
X_train_tfidf = tfidf.fit_transform(X_train)

print("Training LinearSVC (Tốt hơn Random Forest cho Text)...")
model = LinearSVC(random_state=42, class_weight='balanced')
model.fit(X_train_tfidf, y_train)

X_test_tfidf = tfidf.transform(X_test)
y_pred = model.predict(X_test_tfidf)
print("Classification Report:")
print(classification_report(y_test, y_pred))

print("Saving optimized models...")
with open('model.pkl', 'wb') as model_file, open('tfidf_vectorizer.pkl', 'wb') as tfidf_file:
    pickle.dump(model, model_file)
    pickle.dump(tfidf, tfidf_file)

print("Optimization Done! Model is ready.")
