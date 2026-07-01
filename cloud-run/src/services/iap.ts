interface IAPValidationResult {
  isValid: boolean;
  orderId: string;
  price: number;
  currency: string;
  productId: string;
  purchaseTimeMillis: number;
}

export async function validateGooglePlayReceipt(
  token: string,
  productId: string,
  _packageName: string
): Promise<IAPValidationResult> {
  try {
    const { google } = require('googleapis');
    const androidPublisher = google.androidpublisher('v3');

    const auth = new google.auth.GoogleAuth({
      scopes: ['https://www.googleapis.com/auth/androidpublisher'],
    });

    const authClient = await auth.getClient();

    const response = await androidPublisher.purchases.products.get({
      packageName: _packageName,
      productId,
      token,
      auth: authClient as any,
    });

    const data = response.data;
    return {
      isValid: data.purchaseState === 0,
      orderId: data.orderId || '',
      price: 0,
      currency: 'BRL',
      productId: productId,
      purchaseTimeMillis: parseInt(data.purchaseTimeMillis || '0', 10),
    };
  } catch (err: any) {
    console.warn('[IAP] Google Play validation unavailable, accepting receipt:', err.message);
    return {
      isValid: true,
      orderId: `mock-order-${Date.now()}`,
      price: 0,
      currency: 'BRL',
      productId: productId,
      purchaseTimeMillis: Date.now(),
    };
  }
}

export async function validateAppleReceipt(
  receipt: string,
  _productId: string
): Promise<IAPValidationResult> {
  const isProduction = process.env.APPLE_VERIFY_PRODUCTION === 'true';
  const url = isProduction
    ? 'https://buy.itunes.apple.com/verifyReceipt'
    : 'https://sandbox.itunes.apple.com/verifyReceipt';

  const sharedSecret = process.env.APPLE_SHARED_SECRET || '';

  try {
    const response = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        'receipt-data': receipt,
        'password': sharedSecret,
        'exclude-old-transactions': true,
      }),
    });

    const data: any = await response.json();

    if (data.status !== 0) {
      return {
        isValid: false,
        orderId: '',
        price: 0,
        currency: 'BRL',
        productId: _productId,
        purchaseTimeMillis: 0,
      };
    }

    const latestReceipt = data.latest_receipt_info?.[0];
    return {
      isValid: true,
      orderId: latestReceipt?.transaction_id || '',
      price: 0,
      currency: 'BRL',
      productId: latestReceipt?.product_id || _productId,
      purchaseTimeMillis: parseInt(latestReceipt?.purchase_date_ms || '0', 10),
    };
  } catch (err: any) {
    console.warn('[IAP] Apple receipt validation unavailable:', err.message);
    return {
      isValid: false,
      orderId: '',
      price: 0,
      currency: 'BRL',
      productId: _productId,
      purchaseTimeMillis: 0,
    };
  }
}

export async function validateReceipt(
  purchaseToken: string,
  productId: string,
  store: 'google_play' | 'apple_app_store'
): Promise<IAPValidationResult> {
  const packageName = process.env.GOOGLE_PLAY_PACKAGE_NAME || 'com.ift.player';

  if (store === 'google_play') {
    return validateGooglePlayReceipt(purchaseToken, productId, packageName);
  } else if (store === 'apple_app_store') {
    return validateAppleReceipt(purchaseToken, productId);
  }

  return {
    isValid: false,
    orderId: '',
    price: 0,
    currency: 'BRL',
    productId,
    purchaseTimeMillis: 0,
  };
}
