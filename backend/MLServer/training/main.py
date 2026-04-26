import pandas as pd
from flask import Flask, request, jsonify
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.ensemble import RandomForestClassifier
from sklearn.model_selection import train_test_split
from sklearn.metrics import classification_report, accuracy_score
import pickle


app = Flask(__name__)


labels = {
    0: "Bình thường",
    1: "Chứa nội dung bạo lực và kích động",
    2: "Ngôn ngữ không phù hợp hoặc xúc phạm",
    3: "Spam hoặc quảng cáo không liên quan",
    4: "Nội dung phân biệt chủng tộc, giới tính hoặc phân biệt đối xử",
    5: "Chứa nội dung khiêu dâm hoặc gây hại"
}


df = pd.read_csv('dataset.txt', delimiter=',', header=0)
X = df['comment_text']
y = df['type']


X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.1, random_state=59, stratify=y)


with open('vietnamese-stopwords.txt', 'r', encoding='utf-8') as f:
    vietnamese_stopwords = [line.strip() for line in f.readlines()]


tfidf = TfidfVectorizer(stop_words=vietnamese_stopwords)
X_train_tfidf = tfidf.fit_transform(X_train)


model = RandomForestClassifier(n_estimators=100, random_state=42)
model.fit(X_train_tfidf, y_train)


X_test_tfidf = tfidf.transform(X_test)
y_pred = model.predict(X_test_tfidf)
print("Classification Report:")
print(classification_report(y_test, y_pred))
print("Accuracy:", accuracy_score(y_test, y_pred))


with open('model.pkl', 'wb') as model_file, open('tfidf_vectorizer.pkl', 'wb') as tfidf_file:
    pickle.dump(model, model_file)
    pickle.dump(tfidf, tfidf_file)


with open('model.pkl', 'rb') as model_file, open('tfidf_vectorizer.pkl', 'rb') as tfidf_file:
    model = pickle.load(model_file)
    tfidf = pickle.load(tfidf_file)


@app.route('/predict', methods=['POST'])
def predict():

    data = request.get_json()

    comment = data['comment']

    print(comment)

    comment_tfidf = tfidf.transform([comment])

    predicted_class = model.predict(comment_tfidf)[0]

    print(predicted_class)

    result = {
        'comment': comment,
        'predicted_class': labels[predicted_class]
    }

    return jsonify(result)


if __name__ == '__main__':

    specified_ip = "192.168.41.110"

    print(f"App is running on: http://{specified_ip}:5000")

    app.run(host=specified_ip, port=2002, debug=True)
