import pickle
import sys
from flask import Flask, request, jsonify

app = Flask(__name__)

model = None
tfidf = None

# Load model and vectorizer
print("Loading model and vectorizer...")
try:
    with open('model.pkl', 'rb') as model_file:
        model = pickle.load(model_file)
    with open('tfidf_vectorizer.pkl', 'rb') as tfidf_file:
        tfidf = pickle.load(tfidf_file)
    # Phải transform được — nếu không, vectorizer chưa fit hoặc .pkl lệch phiên bản
    _ = tfidf.transform(["sanity_check"])
    _ = model.predict(_)
    print("Models loaded successfully (TF-IDF + classifier OK).")
except Exception as e:
    print(f"Error loading models: {e}", file=sys.stderr)
    print(
        "Gợi ý: chạy lại `python retrain_optimized.py` trong thư mục MLServer rồi build image lại.",
        file=sys.stderr,
    )
    sys.exit(1)

labels = {
    0: "Bình thường",
    1: "Chứa nội dung bạo lực và kích động",
    2: "Ngôn ngữ không phù hợp hoặc xúc phạm",
    3: "Spam hoặc quảng cáo không liên quan",
    4: "Nội dung phân biệt chủng tộc, giới tính hoặc phân biệt đối xử",
    5: "Chứa nội dung khiêu dâm hoặc gây hại"
}

@app.route('/predict', methods=['POST'])
def predict():
    try:
        data = request.get_json()
        if not data or 'comment' not in data:
            return jsonify({'error': 'Missing comment field'}), 400

        comment = data['comment']
        print(f"Received comment: {comment}")

        # Vectorize the comment
        comment_tfidf = tfidf.transform([comment])

        # Predict
        predicted_class = int(model.predict(comment_tfidf)[0])
        print(f"Predicted class: {predicted_class}")

        result = {
            'comment': comment,
            'predicted_class_id': predicted_class,
            'predicted_class_name': labels.get(predicted_class, "Unknown")
        }

        return jsonify(result), 200
    except Exception as e:
        return jsonify({'error': str(e)}), 500

if __name__ == '__main__':
    # Use 0.0.0.0 to bind to all interfaces for Docker
    print("Starting ML Server on port 2002...")
    app.run(host="0.0.0.0", port=2002, debug=False)
