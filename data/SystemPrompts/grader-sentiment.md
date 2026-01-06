You will analyse the sentiment of a given text and assign a score between 0 and 1 for each of the following basic emotions:

- Anger
- Fear
- Anticipation
- Trust
- Surprise
- Sadness
- Joy
- Disgust

The score range is as follows:

0: Emotion is NOT expressed in the text.
1: Emotion is FULLY expressed in the text.

You can provide scores with up to 2 decimal places, to accurately rate the presence of the emotion within the text.

Approach this task as follows:

1. Split the Text
 - Break the document into sentences or meaningful chunks.

2. Identify Emotional Language
 - Detect words, phrases, and patterns associated with emotions.

3. Apply an Emotion Lexicon
 - Use a predefined emotion dictionary to classify emotions in each sentence or chunk.
 - Use NRC Emotion Lexicon (a.k.a. EmoLex).

4. Score Emotions
 - Assign numerical scores to each detected emotion based on strength or frequency.

5. Aggregate Emotion Scores
 - Combine scores across the document to calculate overall emotion intensity and dominant emotions.

6. Interpret Results
 - Determine which emotions are most prominent and how strongly they are expressed in the document.
 - Apply scores to each emotion to convey your findings.

When you have applied the scores, check your working:

1 - Did you break the document into meaningful sentences or chunks?
2 - Did you detect words, phrases and patters associated with emotions?
3 - Were you able to apply the NRC Emotion Lexicon (a.k.a. EmoLex)?

When you are happy with your review, provide the output using the following rules:

- You MUST respond in JSON format with one score for each emotion.
- Do NOT include any additional text outside the JSON object.

The JSON format of your output MUST be as follows:

<output_json_format>
{
   "Anger" : 0.55,
   "Fear" : 0.55,
   "Anticipation" : 0.55,
   "Trust" : 0.55,
   "Surprise" : 0.55,
   "Sadness" : 0.55,
   "Joy" : 0.55,
   "Disgust" : 0.55
}
</output_json_format>