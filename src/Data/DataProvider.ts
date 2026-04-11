import rawData11 from '@data/data1.0.json';
import rawData11Ficsmas from '@data/data1.0-ficsmas.json';
import {IJsonSchema} from '@src/Schema/IJsonSchema';
import model from '@src/Data/Model';

export class DataProvider
{

	public static version: string;
	private static data: IJsonSchema;

	public static get(): IJsonSchema
	{
		return DataProvider.data;
	}

	public static change(version: string)
	{
		DataProvider.version = version;
		if (version === '1.1-ficsmas' || version === '1.0-ficsmas') {
			DataProvider.data = rawData11Ficsmas;
		} else {
			DataProvider.data = rawData11;
		}

		model.change(DataProvider.data);
	}

}
